# AI 協助開發說明文件
## MachineMonitor 機台監控 Dashboard

---

## 一、使用工具

| 項目 | 內容 |
|------|------|
| AI 工具 | Claude Code（Anthropic Claude Sonnet）|
| 互動方式 | 對話式指令，逐步描述需求，由 AI 生成或修正程式碼 |
| 開發環境 | Windows 11、.NET 10、Visual Studio Code |

---

## 二、AI 協助的開發步驟

### Step 1 — 系統架構設計

**我的提問方向：**

> 「我要用 ASP.NET Core 做一個機台監控 Dashboard，資料來自 JSON 檔，希望檔案更新時網頁自動刷新，請幫我規劃整體架構。」

**AI 提供的架構建議：**

AI 說明了三層分工：
- `MachineDataService`（Singleton）— 負責把 JSON 讀進記憶體，集中管理
- `FileWatcherService`（IHostedService）— 背景監聽 `data/` 資料夾變動
- `MachineHub`（SignalR Hub）— 即時推送訊息給前端

AI 同時解釋了為什麼 `MachineDataService` 要用 `AddSingleton` 而非 `AddScoped`：因為資料從 JSON 讀入記憶體後整個 App 共用一份，`AddScoped` 每次 Request 會建立新實體，導致資料不一致。

這個架構決定節省了我自己摸索的時間，也讓我理解了各層的職責邊界。

---

### Step 2 — 後端程式碼生成

#### 2-1 Models/Machine.cs

**提問：**
> 「機台 JSON 的格式如下，幫我建立對應的 C# Model，欄位包含機器 ID、名稱、類型、位置、狀態、圖片 key、量測資料（陣列）。」

AI 生成了含 `[JsonPropertyName]` 屬性的 `Machine` 與 `Measurement` 兩個類別，讓 JSON key（小寫駝峰）能正確對應 C# 屬性（Pascal Case）。

```csharp
// AI 生成的 Model 結構
public class Machine {
    [JsonPropertyName("machineId")]   public string MachineId  { get; set; } = "";
    [JsonPropertyName("measurements")] public List<Measurement> Measurements { get; set; } = new();
    // ...
}
```

#### 2-2 Services/MachineDataService.cs

**提問：**
> 「幫我實作 MachineDataService，需要 LoadAll（讀全部 JSON）、UpdateFromFile（只更新單一台）、GetAll（執行緒安全地回傳清單）。」

AI 生成了含 `lock(_lock)` 的實作，並說明為什麼 `GetAll()` 要用 `.ToList()` 複製一份再回傳（避免呼叫端拿到原始 List 後造成 race condition）：

```csharp
public List<Machine> GetAll() {
    lock (_lock)
        return _machines.ToList(); // 複製一份，外部修改不影響內部
}
```

#### 2-3 Services/FileWatcherService.cs

**提問：**
> 「FileWatcherService 要實作 IHostedService，監控 data/ 資料夾的 machine*.json，有變動就更新 Service 並推 SignalR。但我擔心讀到還在寫入的檔案怎麼辦？」

AI 說明了加 `Task.Delay(300)` 的原因：`FileSystemWatcher` 觸發時檔案可能還在被寫入，延遲 300ms 等寫完再讀，避免讀到損毀的 JSON。

```csharp
private async void OnFileChanged(object sender, FileSystemEventArgs e) {
    await Task.Delay(300); // 等檔案寫完，避免讀到寫到一半的內容
    _dataService.UpdateFromFile(e.FullPath);
    await _hub.Clients.All.SendAsync("MachineUpdated");
}
```

#### 2-4 Program.cs 串接設定

**提問：**
> 「幫我把所有服務在 Program.cs 正確串起來，包含 SignalR 路由和靜態檔案。」

AI 生成了最終的 `Program.cs`，說明 `AddSingleton` 與 `AddHostedService` 的啟動時機差異，以及 `UseDefaultFiles()` 讓 `index.html` 成為預設首頁的原理。

---

### Step 3 — 圖片管理設計

**我遇到的問題：**

題目要求「機器圖檔（二進位格式）」，但如果每台機器 JSON 都存 Base64，15 台同類型機台就會重複儲存 15 份相同圖片，造成資料冗餘。

**提問：**
> 「5 種機台類型，15 台機器，圖片如果每台 JSON 都存 Base64 很浪費，有沒有更好的方式？」

**AI 建議的解法：**

把圖片集中管理在 `data/images.json`，key 用機台類型的英文 ID，機台 JSON 只存 key：

```json
// data/images.json（集中管理，只存一份）
{
  "compressor": "PHN2ZyB4bWxucy...",
  "motor":      "PHN2ZyB4bWxucy...",
  "pump":       "PHN2ZyB4bWxucy...",
  "fan":        "PHN2ZyB4bWxucy...",
  "heat_exchanger": "PHN2ZyB4bWxucy..."
}
```

```json
// machine01.json（只存圖片 key）
{
  "machineId": "machine_01",
  "imageBase64": "compressor",
  ...
}
```

前端組合時：
```javascript
const imgSrc = `data:image/svg+xml;base64,${images[m.imageBase64]}`;
```

這個設計讓新增機台時完全不需要管圖片，只有新增機台「類型」才需要更新 `images.json`。

---

### Step 4 — 測試資料生成

**提問：**
> 「幫我生成 15 台機器的 JSON 測試檔，涵蓋 5 種機台類型（壓縮機、馬達、水泵、風機、熱交換器）、3 種狀態（良好、異常、危險）、4 個廠區，每台包含 10 筆量測資料。」

AI 一次生成了 15 個格式一致的 JSON 檔（machine01.json ～ machine15.json），每台機器的量測值各自有合理的隨機波動，符合真實場景。

---

### Step 5 — 前端 Dashboard 開發

**提問：**
> 「幫我用純 HTML + Tailwind CSS 做一個深色風格的機台監控 Dashboard，需要：機台卡片格、依狀態/類型/廠區篩選、點卡片開 Modal 顯示 ECharts 趨勢圖（含可移動游標）、SignalR 連線並在收到更新時自動重新載入資料。」

AI 逐段生成了 `index.html`，包含：

**狀態統計列：**
```html
<!-- 頂端即時顯示總數、良好、異常、危險台數 -->
<div id="cTotal">—</div>
<div id="cGood">—</div>
```

**危險機台動態邊框效果：**
```css
/* AI 設計的 CSS，危險狀態機台有紅色呼吸燈效果 */
.card-danger {
  animation: danger-pulse 2s ease-in-out infinite;
}
```

**ECharts 趨勢圖含游標（crosshair）：**
```javascript
// AI 生成的 ECharts 設定，axisPointer 提供可移動游標
tooltip: {
  trigger: 'axis',
  axisPointer: { type: 'cross' }
}
```

**SignalR 前端連線：**
```javascript
const conn = new signalR.HubConnectionBuilder().withUrl('/machineHub').build();
conn.on('MachineUpdated', () => loadData()); // 收到推送 → 重新載入
```

---

### Step 6 — 問題排除

#### 問題 1：SignalR 注入方式

**遇到的錯誤：**
`FileWatcherService` 無法直接 `new MachineHub()` 來推訊息。

**提問：**
> 「我在 FileWatcherService 要推 SignalR，但不能直接 new MachineHub，怎麼做？」

**AI 解答：**
要透過 `IHubContext<MachineHub>` 注入，這是 ASP.NET Core DI 的標準方式，`IHubContext` 提供了在 Hub 連線外部呼叫 `Clients.All.SendAsync()` 的能力。

```csharp
public FileWatcherService(MachineDataService dataService, IHubContext<MachineHub> hub, ...)
{
    _hub = hub; // 正確方式：透過 DI 注入 IHubContext
}
```

#### 問題 2：ImagesController 設計

**提問：**
> 「images.json 放在 data/ 資料夾（不是 wwwroot），前端要怎麼取得？」

**AI 建議：** 建立 `ImagesController`，讀取 `data/images.json` 回傳，讓前端用 `/api/images` 取得，符合 API 設計慣例且不需要把圖片 Base64 暴露在靜態路徑。

---

## 三、AI 協助的效益總結

| 開發項目 | 沒有 AI 預估時間 | 有 AI 實際時間 | 節省比例 |
|----------|-----------------|---------------|---------|
| 系統架構設計 | 2～3 小時 | 30 分鐘 | ~80% |
| 後端 Service 實作 | 3～4 小時 | 1 小時 | ~75% |
| 執行緒安全（lock）設計 | 反覆 debug | AI 直接說明 | — |
| 前端 Dashboard UI | 4～6 小時 | 1.5 小時 | ~75% |
| ECharts 趨勢圖設定 | 2 小時（看文件） | 20 分鐘 | ~83% |
| 15 台測試 JSON 生成 | 1 小時（手動） | 5 分鐘 | ~92% |

**AI 最大的價值不只是生成程式碼，而是在每個決策點提供理由說明**（為什麼用 Singleton、為什麼要 lock、為什麼要 Delay 300ms），讓我同時學會了背後的設計原理，而不只是複製貼上。

---

## 四、開發過程對話範例

以下為實際與 AI 互動的典型對話模式：

**架構討論：**
```
我：FileWatcherService 要怎麼在 App 啟動時就自動執行，
    不是等人呼叫才跑？

AI：要用 AddHostedService<FileWatcherService>，
    ASP.NET Core 會在 App 啟動時自動呼叫 StartAsync()。
    如果你用 AddSingleton，它只有第一次被注入時才會建構，
    無法保證啟動順序。
```

**Debug 協助：**
```
我：FileSystemWatcher 觸發後去讀檔案，有時會拋出 IOException，
    說檔案被鎖定，怎麼解決？

AI：這是因為觸發事件時，外部程式（或你的編輯器）還在寫入。
    加上 await Task.Delay(300) 等 300ms 讓寫入完成再讀，
    這是業界常見的 workaround。
```

---

## 五、專案最終架構

```
MachineController  ──→  MachineDataService（Singleton, lock 保護）
FileWatcherService ──→  MachineDataService（UpdateFromFile）
FileWatcherService ──→  IHubContext<MachineHub>（推送 SignalR）
MachineDataService ──→  IWebHostEnvironment（取得 data/ 路徑）
ImagesController   ──→  data/images.json（集中圖片管理）
```

整個專案從架構設計、後端實作、前端 UI 到測試資料，全程以 AI 作為主要協作工具，大幅縮短了開發時間，同時透過 AI 的逐步解釋加深了對 ASP.NET Core DI、執行緒安全、SignalR 的理解。
