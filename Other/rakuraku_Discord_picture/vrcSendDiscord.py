import sys
import shutil
import datetime

class vrcSendDiscord:
    def __init__(self,getfldpath,dt_now,getApiDiscord):
        self.getfldpath = getfldpath#
        self.dt_now = dt_now
        self.yearmonth_now=f"{dt_now.year}{dt_now.month:02d}"
        self.getApiDiscord = getApiDiscord

    def getfld(self,yermon_now):
        print(f"{self.dt_now}:{yermon_now}")
        return  
    
    def setWebhook(self):
        print("Webhook")
        return
    def check_fldstatus(self):
        return

dt_now = datetime.datetime.now()
vrcSendDiscord_instance = vrcSendDiscord(r"C:\Users\kenny\Pictures\VRChat"
                                
                                ,"https://discord.com/api/webhooks/")

vrcSendDiscord_instance.getfld(vrcSendDiscord_instance.yearmonth_now)
