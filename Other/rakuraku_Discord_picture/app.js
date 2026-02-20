'use strict';

const logFileName = 'sent_files_log.json';
let webhookUrl = localStorage.getItem('savedWebhookUrl') || '';

// Discordの上限は状況により変動/オーバーヘッドもあるので少し余裕を持たせる
const MAX_DISCORD_PAYLOAD_SIZE = 24 * 1024 * 1024;
const MAX_FILES_PER_REQUEST = 10;

function $(id) {
  return document.getElementById(id);
}

function setWebhookStatus(message) {
  $('webhookStatus').textContent = message;
}

function updateStatusMessage(message) {
  $('statusMessage').textContent = message;
}

function updateSelectedFolderName(folderName) {
  $('selectedFolderName').textContent = `Selected folder: ${folderName}`;
}

function updateSentFilesList(sentFiles) {
  const sentFilesList = $('sentFilesList');
  sentFilesList.innerHTML = '';
  sentFiles.forEach(fileName => {
    const listItem = document.createElement('li');
    listItem.textContent = fileName;
    sentFilesList.appendChild(listItem);
  });
}

function updateSkippedFilesList(skippedFiles) {
  const skippedFilesList = $('skippedFilesList');
  skippedFilesList.innerHTML = '';
  skippedFiles.forEach(fileName => {
    const listItem = document.createElement('li');
    listItem.textContent = fileName;
    skippedFilesList.appendChild(listItem);
  });
}

async function getLog() {
  const log = localStorage.getItem(logFileName);
  return log ? JSON.parse(log) : {};
}

async function saveLog(log) {
  localStorage.setItem(logFileName, JSON.stringify(log));
}

function groupFilesBySizeAndCount(files) {
  const groups = [];
  let currentGroup = [];
  let currentSize = 0;

  for (const file of files) {
    if (file.size > MAX_DISCORD_PAYLOAD_SIZE) {
      console.warn(`File "${file.name}" exceeds the maximum allowed size and will be skipped.`);
      continue;
    }

    if (currentSize + file.size > MAX_DISCORD_PAYLOAD_SIZE || currentGroup.length >= MAX_FILES_PER_REQUEST) {
      if (currentGroup.length > 0) groups.push(currentGroup);
      currentGroup = [];
      currentSize = 0;
    }

    currentGroup.push(file);
    currentSize += file.size;
  }

  if (currentGroup.length > 0) {
    groups.push(currentGroup);
  }

  return groups;
}

async function sendFolderInfoToDiscord(folderName) {
  const currentDate = new Date().toLocaleString();
  const payload = {
    content: `Selected folder: ${folderName}\nTimestamp: ${currentDate}`
  };

  const response = await fetch(webhookUrl, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    throw new Error(`Failed to send folder info. Status: ${response.status}`);
  }
}

async function sendFilesToDiscord(files) {
  const log = await getLog();
  const forceSend = $('forceSend').checked;

  const sentFiles = [];
  const skippedFiles = [];

  const filesToSend = Array.from(files).filter(file => {
    // ログキー衝突回避: フォルダ階層も含める
    const keyPath = file.webkitRelativePath || file.name;
    const fileKey = `${keyPath}_${file.lastModified}`;

    if (!forceSend && log[fileKey]) {
      skippedFiles.push(file.name);
      return false;
    }
    return true;
  });

  const fileGroups = groupFilesBySizeAndCount(filesToSend);
  let groupCounter = 0;
  const totalGroups = fileGroups.length;

  for (const group of fileGroups) {
    updateStatusMessage(`Sending group ${groupCounter + 1} of ${totalGroups}`);

    const formData = new FormData();
    group.forEach((file, index) => {
      formData.append(`files[${index}]`, file, file.name);
    });

    try {
      const response = await fetch(webhookUrl, { method: 'POST', body: formData });
      if (response.ok) {
        group.forEach(file => {
          const keyPath = file.webkitRelativePath || file.name;
          const fileKey = `${keyPath}_${file.lastModified}`;
          log[fileKey] = true;
          sentFiles.push(file.name);
        });
      } else {
        console.error(`Failed to send group ${groupCounter + 1}. Status: ${response.status}`);
      }
    } catch (error) {
      console.error(`Error sending group ${groupCounter + 1}`, error);
    }

    groupCounter++;
  }

  await saveLog(log);
  updateSentFilesList(sentFiles);
  updateSkippedFilesList(skippedFiles);
  updateStatusMessage(`Finished. Sent: ${sentFiles.length}, Skipped: ${skippedFiles.length}`);
}

function init() {
  $('webhookInput').value = webhookUrl;

  // 初期状態: webhookが保存済みなら送信ボタン有効化
  if (webhookUrl) {
    $('sendFolderButton').disabled = false;
    setWebhookStatus('Webhook URL loaded from saved settings.');
  } else {
    $('sendFolderButton').disabled = true;
    setWebhookStatus('');
  }

  $('saveWebhookButton').addEventListener('click', () => {
    webhookUrl = $('webhookInput').value.trim();
    if (webhookUrl) {
      localStorage.setItem('savedWebhookUrl', webhookUrl);
      setWebhookStatus('Webhook URL saved.');
      $('sendFolderButton').disabled = false;
      updateStatusMessage('Ready.');
    } else {
      updateStatusMessage('Please enter a valid Webhook URL.');
    }
  });

  $('sendFolderButton').addEventListener('click', async () => {
    const files = $('folderInput').files;

    if (!webhookUrl) {
      updateStatusMessage('Webhook URL is not set.');
      return;
    }

    if (!files || files.length === 0) {
      updateStatusMessage('No files selected.');
      return;
    }

    try {
      const folderName = files[0].webkitRelativePath
        ? files[0].webkitRelativePath.split('/')[0]
        : 'Selected files';

      updateStatusMessage('Sending folder info and files...');
      await sendFolderInfoToDiscord(folderName);
      await sendFilesToDiscord(files);
    } catch (error) {
      console.error(error);
      updateStatusMessage('Error occurred. Check console for details.');
    }
  });

  $('folderInput').addEventListener('change', () => {
    const files = $('folderInput').files;
    if (files && files.length > 0 && files[0].webkitRelativePath) {
      updateSelectedFolderName(files[0].webkitRelativePath.split('/')[0]);
    } else if (files && files.length > 0) {
      updateSelectedFolderName('Selected files');
    } else {
      updateSelectedFolderName('No folder selected');
    }
  });
}

document.addEventListener('DOMContentLoaded', init);
