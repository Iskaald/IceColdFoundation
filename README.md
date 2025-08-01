# IceCold Core Module for Unity

A lightweight, foundational framework for Unity projects designed to streamline service management, configuration, and logging. It provides a robust, auto-initializing core that handles dependencies and lifecycle, allowing you to focus on building game features.

This is the **Core** module, which serves as the foundation for all other IceCold modules.

## Key Features

*   **Automatic Service Management:** A zero-setup service locator that automatically discovers, initializes, and manages services throughout your application's lifecycle.
*   **Powerful Configuration System:** Leverages `ScriptableObject`s for clean, designer-friendly configuration, with built-in support for importing/exporting data.
*   **Google Sheets Integration:** Directly import configuration data from a public Google Sheet, allowing for real-time collaboration and data management outside of the Unity Editor.
*   **Flexible Logging:** A centralized logger that can be configured to show or hide logs based on the build type (Editor, Debug, Release), keeping your console clean.

---

## Installation & Management

The recommended way to install and manage the IceCold framework and its modules is through the official **IceCold Installer**. The installer provides a user-friendly interface to add, remove, and update modules, and it automatically handles all dependencies.

### 1. Install the IceCold Installer

First, you need to add the installer package to your project.

1.  In your Unity project, open the Package Manager window (**Window > Package Manager**).
2.  Click the **+** icon in the top-left corner and select **Add package from git URL...**.
3.  Paste the following URL and click **Add**:
    ```
    https://github.com/Iskaald/IceColdInstaller.git
    ```

The installer window should open automatically the first time it's installed. You can also open it any time via the menu: **IceCold > Installer**.

### 2. Install the Core Module

Once the installer is open:

1.  Find **IceCold Core** in the list of available modules.
2.  Select the desired version from the dropdown menu.
3.  Click **Install**. The installer will add the package to your project and automatically resolve any dependencies.

### 3. Setup Project Folders

After installing the Core module, you need to create the necessary folder structure for configuration assets.

1.  In the Installer window, click the **Setup** button in the bottom-right corner.
2.  This will create the `Assets/IceCold/Settings/Resources` folder structure if it doesn't already exist.

---

## 1. Service Management (`Core`)

The heart of the framework is the `Core` static class, which manages the lifecycle of all services. A service is any class that implements the `IIceColdService` interface.

### Creating a Service

To create a new service, simply create a new C# class that implements `IIceColdService`.

*   **`Initialize()`**: Called once on startup. Use this for setup and caching references.
*   **`Deinitialize()`**: Called once when the application quits. Use this for cleanup.
*   **`OnWillQuit()`**: A `virtual` method called just before the application quits. **By default, it returns `true` and allows the application to close.** You only need to override this method if you want to temporarily block the quitting process (by returning `false`), for instance, to show a "Save your changes?" dialog. Since it's not abstract, you don't have to implement it in every service.
*   **`[ServicePriority(int)]`**: (Optional) Use this attribute to control the initialization order. Lower numbers initialize first. This is crucial for services that depend on each other. The `IceColdLoggerService` has a priority of `0` to ensure it's available for all other services.

**Example: `PlayerStatsService.cs`**
```csharp
using IceCold.Interface;

[ServicePriority(100)] // Initializes after the Logger (priority 0)
public class PlayerStatsService : IIceColdService
{
    public int PlayerLevel { get; private set; }

    public void Initialize()
    {
        PlayerLevel = 1;
        IceColdLogger.Log("PlayerStatsService Initialized!");
    }

    public void LevelUp()
    {
        PlayerLevel++;
        IceColdLogger.Log($"Player leveled up to {PlayerLevel}!");
    }

    public void Deinitialize() { /* Cleanup if needed */ }
    // No need to implement OnWillQuit() if you want the default behavior (allow app to quit).
}
```

### Accessing a Service

Services are retrieved using the `Core.GetService<T>()` method from anywhere in your code.

```csharp
void SomeMonobehaviourMethod()
{
    var statsService = Core.GetService<PlayerStatsService>();
    statsService.LevelUp();
}
```

---

## 2. Configuration System (`IceColdConfig`)

This system uses `ScriptableObject`s to manage configuration data. All configs must inherit from the abstract `IceColdConfig` class.

### Creating a Config Asset

1.  Create a new C# script that inherits from `IceColdConfig`.
2.  Add a `[CreateAssetMenu]` attribute to make it easy to create in the editor.
3.  Implement the abstract `Key` property. It's recommended to use `nameof(YourConfigClass)`.
4.  Place the created asset inside the `Assets/IceCold/Settings/Resources` folder.

**Example: `GameSettingsConfig.cs`**
```csharp
using IceCold.Interface;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(GameSettingsConfig), menuName = "IceCold/Game Settings", order = 1)]
public class GameSettingsConfig : IceColdConfig
{
    public override string Key => nameof(GameSettingsConfig);

    public string gameVersion = "1.0.0";
    public float musicVolume = 0.8f;
    public bool enableTutorial = true;
}
```

### Using a Config

Access your config data in any service or script using the static `GetConfig<T>()` method. The system will load it from `Resources` or create a temporary in-memory instance if it's not found.

```csharp
public void Initialize()
{
    var settings = IceColdConfig.GetConfig<GameSettingsConfig>();
    AudioListener.volume = settings.musicVolume;
    IceColdLogger.Log($"Game Version: {settings.gameVersion}");
}
```

### Google Sheets Integration

This feature allows you to manage your config data in a Google Sheet and import it directly into your `ScriptableObject` asset.

#### Setup

1.  **Create API Key (Optional but Recommended):** To automatically fetch the list of tabs from your sheet, you need a Google Cloud API Key.
    *   Go to the [Google Cloud Console](https://console.cloud.google.com/).
    *   Create a new project.
    *   Go to "APIs & Services" > "Credentials".
    *   Click "Create Credentials" > "API key".
    *   Go to "APIs & Services" > "Library" and enable the **Google Sheets API**.
    *   Copy the generated API key.
2.  **Create Google Sheets Config:** In Unity, go to **Assets > Create > IceCold > Google Sheets Config**.
3.  **Enter API Key:** Paste your API key into the `Api Key` field of the `GoogleSheetsConfig` asset.
4.  **Share Your Sheet:** In Google Sheets, click **Share** and change the general access to **Anyone with the link can view**.

#### Usage

1.  Select your config asset (e.g., `GameSettingsConfig.asset`).
2.  Check the **Import from Google Sheets** box.
3.  Paste your Google Sheet URL into the **Google Sheet URL** field.
4.  **Workflow with API Key:**
    *   Click **Refresh tabs**.
    *   Select the desired tab from the **Sheet Tab** dropdown.
    *   Click **Download & Import from Selected Tab**.
5.  **Workflow without API Key:**
    *   A warning will appear.
    *   Manually type the **exact name** of the sheet tab you want to import into the **Sheet Tab Name** field.
    *   Click **Download & Import from Tab**.

> **Note:** The column headers in your Google Sheet must **exactly match** the field names in your config class (e.g., `gameVersion`, `musicVolume`).

### CSV Export

You can export the current state of any config asset to a local `.csv` file by clicking the **Export to CSV File** button in its inspector.

---

## 3. Logging System (`IceColdLogger`)

A static wrapper around `Debug.Log` that allows you to filter messages based on the build environment.

### Configuration

The logger's behavior is controlled by the `LoggerConfig` asset.

1.  To create or find it, use the menu item **IceCold > Logger > Config**.
2.  The asset contains three filter settings:
    *   **Debug Filter Settings:** Used in Development Builds.
    *   **Release Filter Settings:** Used in Release (non-development) Builds.
    *   **Editor Filter Settings:** Used when running in the Unity Editor.
3.  For each setting, you can toggle `log`, `warning`, and `error` messages on or off. By default, regular logs are disabled in release builds to avoid spamming the player's log files.

### Usage

Use the logger just like you would use `Debug.Log`. The `IceColdLoggerService` is automatically initialized with the highest priority (`0`), so it's safe to use from any other service's `Initialize` method.

```csharp
// These messages will be filtered based on your LoggerConfig settings.
IceColdLogger.Log("This is a standard log message.");
IceColdLogger.LogWarning("This is a warning.");
IceColdLogger.LogError("This is an error.");

try
{
    // ... some risky code
}
catch (System.Exception e)
{
    IceColdLogger.LogException(e);
}
```