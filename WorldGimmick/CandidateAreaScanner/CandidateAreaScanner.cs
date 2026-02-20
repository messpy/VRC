
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Sagira.Lottery
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class CandidateAreaScanner : UdonSharpBehaviour
    {
        [SerializeField, HeaderAttribute("このコライダーに重なっているプレイヤーが抽選対象です。")]
        private Collider scanAreaCollider;

        /// <summary>
        /// スキャンエリア内のプレイヤーをスキャンする
        /// </summary>
        /// <returns>スキャンエリア内のプレイヤーの名前リスト</returns>

        public void Update()
        {
            Debug.Log("CandidateAreaScanner is ready.");
            ScanPlayers();
        }
        public string[] ScanPlayers()
        {

            VRCPlayerApi[] allPlayers = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(allPlayers);

            string[] candidatePlayerNameList = new string[VRCPlayerApi.GetPlayerCount()];

            int i = 0;
            foreach (var player in allPlayers)
            {
                if (scanAreaCollider.bounds.Contains(player.GetPosition()))
                {
                    candidatePlayerNameList[i] = player.displayName;
                    i++;
                }
            }

            // 空の要素を削除
            string[] result = new string[i];
            for (int j = 0; j < i; j++)
            {
                result[j] = candidatePlayerNameList[j];
            }
            Debug.Log("スキャンエリア内のプレイヤー数: " + i);
            Debug.Log(result);

            return result;
        }
    }

}
