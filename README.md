**必要元件:**  
1..NET Framework 4.5.2以上。  
2.Windows Server 2008 R2以上。  
3.IIS 7.5以上。  

**功能:**  
1.系統啟動中可更新工作排程後再手動載入最新排程。   
2.系統啟動中可更新工作定義後再手動載入最新定義。  

**環境準備:**  
1.IIS安裝【應用程式初始化】模組。  
2.IIS應用程式【預先載入已啟用】設為*True*。  
3.IIS應用程式集區【啟動模式】設為*AlwaysRunning*。  
4.IIS應用程式集區【閒置逾時(分)】設為*0*。  
5.IIS應用程式集區【回收】>【固定時間間隔(分鐘)】設為*0*。  