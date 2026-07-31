using System.Collections;
using UnityEngine;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.IO;

// © 2024 Johannes Lindner <johannes.lindner@tum.de>

public class SumoStarter : MonoBehaviour
{
    [Header("Control")]
    [SerializeField]
    bool startSumoOnStart = true;

    [Header("Debug")]
    [SerializeField]
    public string ProcessID;
    public string error = null;
    public float dt = 0.1f;

    private Thread sumoThread { get; set; }
    private Process process { get; set; }
    private static string markerFilePath;

    void Awake()
    {
        string unityWorkspacePath = Path.GetDirectoryName(Application.dataPath);
        markerFilePath = Path.Combine(unityWorkspacePath, "sumo_bridge.pid");
    }

    // --- This section was adjusted using AI assistance ---
    // EXTERNAL_SUMO: when true, an external TraCI controller is the SUMO host and
    // Unity only connects to port 25001. That mode is not used by this project.
    // With this flag false, pressing Play makes Unity launch Sumonity's own
    // socketServer.py, which starts sumo-gui on the static Darmstadt config
    // (sumoProject/opensource.sumocfg) and serves vehicle data on 25001 itself.
    const bool EXTERNAL_SUMO = false;

    void Start()
    {
        if (EXTERNAL_SUMO)
        {
            UnityEngine.Debug.Log("[SumoStarter] EXTERNAL_SUMO mode: SUMO is hosted by an external TraCI controller on port 25001. Not launching a local SUMO bridge.");
            return;
        }

        if (startSumoOnStart)
        {
            // Clean up any existing processes before starting
            CleanupExistingProcesses();
            StartSumoThread();
        }
    }


    public void StartSumoThread()
    {
        // Initialize Thread
        ThreadStart threadStart = new ThreadStart(StartSumo);
        sumoThread = new Thread(threadStart);
        sumoThread.Start();
    }


    void StartSumo()
    {
        string unityWorkspacePath = Path.GetDirectoryName(Application.dataPath);
        string scriptPath = Path.Combine(unityWorkspacePath, "Assets/Sumonity/SumoTraCI/socketServer.py");
        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError($"SUMO socket server script not found at {scriptPath}");
            error = "socketServer.py missing";
            return;
        }
        string dtValue = dt.ToString(new CultureInfo("en-US"));

        string[] pythonCandidates = new[]
        {
            Path.Combine(unityWorkspacePath, "Assets/Sumonity/SumoTraCI/venv/Scripts/python.exe"),
            Path.Combine(unityWorkspacePath, "Assets/Sumonity/SumoTraCI/venv/bin/python"),
            "python",
            "python3"
        };

        Process startedProcess = null;
        string selectedPython = null;
        bool fromVenv = false;
        string lastErrorMessage = null;

        foreach (string candidate in pythonCandidates)
        {
            if (string.IsNullOrEmpty(candidate))
            {
                continue;
            }

            bool isFilePath = candidate.Contains(Path.DirectorySeparatorChar.ToString()) || candidate.Contains("/");
            if (isFilePath && !File.Exists(candidate))
            {
                continue;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = candidate,
                Arguments = $"\"{scriptPath}\" --dt {dtValue}",
                WorkingDirectory = unityWorkspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process trialProcess = new Process
            {
                StartInfo = startInfo
            };

            try
            {
                trialProcess.Start();
                startedProcess = trialProcess;
                selectedPython = candidate;
                fromVenv = isFilePath;
                break;
            }
            catch (System.Exception ex)
            {
                lastErrorMessage = ex.Message;
                try
                {
                    trialProcess.Dispose();
                }
                catch
                {
                }
            }
        }

        if (startedProcess == null)
        {
            UnityEngine.Debug.LogError($"Failed to start SUMO bridge. Last error: {lastErrorMessage}");
            error = lastErrorMessage ?? "Unable to start python process";
            return;
        }

        if (fromVenv)
        {
            UnityEngine.Debug.Log($"Starting SUMO bridge with bundled virtual environment: {selectedPython}");
        }
        else
        {
            UnityEngine.Debug.LogWarning($"Using global python interpreter for SUMO bridge: {selectedPython}");
        }

        process = startedProcess;
        ProcessID = process.Id.ToString();
        WriteMarkerFile(process.Id);

        int errorCount = 0;
        int maxErrorsToLog = 5;
        
        bool activateDebug = false;

        while (!process.HasExited)
        {
            string output = process.StandardOutput.ReadLine();
            if (!string.IsNullOrEmpty(output))
            {
                try 
                {
                    // Log the output
                    if (activateDebug)
                    {
                        UnityEngine.Debug.Log(output);
                    }
                }
                catch (System.Exception ex)
                {
                    // Only log a limited number of errors to avoid spam
                    if (errorCount < maxErrorsToLog)
                    {
                        UnityEngine.Debug.LogError($"Error processing output: {ex.Message}");
                        errorCount++;
                    }
                    else if (errorCount == maxErrorsToLog)
                    {
                        UnityEngine.Debug.LogWarning("Suppressing further similar errors to avoid spam");
                        errorCount++;
                    }
                }
            }
        }

        error = process.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(error))
        {
            UnityEngine.Debug.LogError(error);
        }
    }


    void OnApplicationQuit()
    {
        // In EXTERNAL_SUMO mode Unity never launched any SUMO/bridge process, so there is
        // nothing of ours to clean up. The generic cleanup below kills EVERY process whose
        // name contains "sumo", which would also kill an external SUMO server every time
        // Play mode was stopped (that server then reports
        // "SUMO closed unexpectedly: connection forcibly closed").
        if (EXTERNAL_SUMO) return;

        CleanupExistingProcesses();
    }

    private void CleanupExistingProcesses()
    {
        UnityEngine.Debug.Log("Cleaning up processes...");

        // Always attempt to clean up an orphaned SUMO Python process from a previous run
        CleanupOrphanedSumoProcessFromMarker();

        // 1. Kill the main process if it exists and hasn't exited
        if (process != null)
        {
            try
            {
                if (!process.HasExited)
                {
                    UnityEngine.Debug.Log($"Killing main process with ID: {process.Id}");
                    process.Kill();
                    process.WaitForExit(3000); // Wait up to 3 seconds for graceful exit
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Error killing main process: {ex.Message}");
            }
            finally
            {
                // Remove marker file when we know the process is gone
                TryDeleteMarkerFile();
                try
                {
                    process.Dispose();
                }
                catch { }
                process = null;
            }
        }

        // 2. Abort the thread if it exists
        if (sumoThread != null && sumoThread.IsAlive)
        {
            try
            {
                UnityEngine.Debug.Log("Aborting SUMO thread");
                sumoThread.Abort();
                sumoThread.Join(2000); // Wait up to 2 seconds for thread to abort
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Error aborting thread: {ex.Message}");
            }
            finally
            {
                sumoThread = null;
            }
        }

        // 3. Close SUMO or SUMO-GUI processes
        UnityEngine.Debug.Log("Closing SUMO or SUMO-GUI processes");
        try
        {
            var processes = Process.GetProcesses();
            foreach (var proc in processes)
            {
                try
                {
                    // Check if process hasn't exited before accessing properties
                    if (!proc.HasExited)
                    {
                        string processName = proc.ProcessName.ToLower();
                        if (processName.Contains("sumo-gui") || processName.Contains("sumo"))
                        {
                            UnityEngine.Debug.Log($"Closing {proc.ProcessName} process with ID: {proc.Id}");
                            proc.Kill();
                            proc.WaitForExit(2000);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    // Skip processes we can't access (access denied, etc.)
                    UnityEngine.Debug.LogWarning($"Could not check/kill process: {ex.Message}");
                }
                finally
                {
                    proc.Dispose();
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"Error during process cleanup: {ex.Message}");
        }

        UnityEngine.Debug.Log("Process cleanup completed");
    }

    private void WriteMarkerFile(int pid)
    {
        if (string.IsNullOrEmpty(markerFilePath))
        {
            return;
        }

        try
        {
            File.WriteAllText(markerFilePath, pid.ToString());
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"Failed to write SUMO Python PID marker: {ex.Message}");
        }
    }

    private void CleanupOrphanedSumoProcessFromMarker()
    {
        if (string.IsNullOrEmpty(markerFilePath))
        {
            return;
        }

        try
        {
            if (!File.Exists(markerFilePath))
            {
                return;
            }

            string pidText = File.ReadAllText(markerFilePath).Trim();
            if (!int.TryParse(pidText, out int pid))
            {
                return;
            }

            Process orphan = null;
            try
            {
                orphan = Process.GetProcessById(pid);
            }
            catch (System.ArgumentException)
            {
                // Process is no longer running
            }

            if (orphan != null)
            {
                using (orphan)
                {
                    if (!orphan.HasExited && orphan.ProcessName.ToLower().Contains("python"))
                    {
                        UnityEngine.Debug.Log($"Killing orphaned SUMO Python process with ID: {orphan.Id}");
                        try
                        {
                            orphan.Kill();
                            orphan.WaitForExit(3000);
                        }
                        catch (System.Exception ex)
                        {
                            UnityEngine.Debug.LogWarning($"Error killing orphaned SUMO Python process: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogWarning($"Error during orphaned SUMO Python cleanup: {ex.Message}");
        }
        finally
        {
            TryDeleteMarkerFile();
        }
    }

    private void TryDeleteMarkerFile()
    {
        if (string.IsNullOrEmpty(markerFilePath))
        {
            return;
        }

        try
        {
            if (File.Exists(markerFilePath))
            {
                File.Delete(markerFilePath);
            }
        }
        catch
        {
            // Ignore errors when deleting the marker file
        }
    }
}
