# Update Directory

# Efficient File Synchronization CLI Tool

UpdateDirectory is a command-line interface (CLI) application designed to intelligently synchronize files and directories, optimizing the process by focusing on copying only the necessary files. It leverages C# 13 and .NET 9 features for performance and maintainability.

## Key Features

* **Fast Hashing and Comparison:** Uses the XXH64 hash algorithm and optimized buffering for rapid file hashing and comparison.
* **Async Operations:** Leverages `async/await` for non-blocking file operations, enhancing responsiveness.
* **Customizable:** Allows configuration of buffer sizes, hash algorithms, and file exclusion filters.

## How it Works

UpdateDirectory calculates and stores hashes for files in both source and destination directories. It then compares these hashes to identify files that are new, modified, or missing in the destination. Only the necessary files are copied, saving time and bandwidth, especially when dealing with large files or frequent updates.

## **Usage**

1. **Install:** 
   
   * Download the latest release from [GitHub Releases](https://github.com/aYiannis/UpdateDirectory).
   * Extract the files to a suitable location.
   * Make sure you have .NET 9 runtime installed on your system.

2. **Run:**
   
   * Open a command prompt or terminal.
   
   * Navigate to the directory where you extracted the UpdateDirectory files.
   
   * Execute the `UpdateDirectory.exe` file with the following arguments:
     
     ```bash
     UpdateDirectory.exe <source_directory> <destination_directory> [options]
     ```
     
     * `<source_directory>`: The path to the directory containing the files you want to synchronize.
     * `<destination_directory>`: The path to the directory where you want to copy the updated files.
     * `[options]`: 
       - `--missing:<value>`: Specify how to handle missing files from the source. Acceptable values: `report, ignore, ask`. (default: `report`, e.g. `--missing:ingore`).
       * `--deep:<true|false>`:  Perform recursive synchronization, including subdirectories. (default: false, e.g. `--deep:false`)
       * `--exclude:<pattern>`: Exclude files matching the specified pattern (default: nothing, e.g. `--exclude:"*.tmp"`).
       * `--simulate:<true|false>`: Will the run be a simulation. Will not update any file, but will produce logs. (default: false, e.g. `--simulate:true`)

## Example

```bash
UpdateDirectory.exe "C:\MySourceFiles" "D:\Backup" --deep --missing:ask
```


## Planed Features

* **Section-Level Hashing (Optional):** Implements block-level hashing for efficient comparison of large files, minimizing unnecessary copying when only parts of a file have changed.