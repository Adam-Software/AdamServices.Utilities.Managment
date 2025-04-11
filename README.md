# AdamServices.Utilities.Managment
[![.NET Build And Publish Release](https://github.com/Adam-Software/AdamServices.Utilities.Managment/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/Adam-Software/AdamServices.Utilities.Managment/actions/workflows/dotnet-desktop.yml)

A utility for downloading, running, and updating AdamServices.* projects.

Use the shared [wiki](https://github.com/Adam-Software/AdamServices.Utilities.Managment/wiki) to find information about the project.

## For users

### Permanent links to releases
* Windows [x64]
  ```
  https://github.com/Adam-Software/AdamServices.Utilities.Managment/releases/latest/download/Managment.win64.portable.zip
  ```
* Linux [arm64]
  ```
  https://github.com/Adam-Software/AdamServices.Utilities.Managment/releases/latest/download/Managment.arm64.portable.zip
  ```
### Install
* Windows [x64]
  * Download using the [permalink](#permanent-links-to-releases)
  * Unzip and run Managmet.exe by specifying the [required command line arguments](#required-command-line-arguments)

* Linux [arm64]
  * Download using the [permalink](#permanent-links-to-releases)
    ```bash
    wget  https://github.com/Adam-Software/AdamServices.Utilities.Managment/releases/latest/download/Managment.arm64.portable.zip
    ```
  * Unzip and make the Management file executable
    ```bash
    unzip Managment.arm64.portable.zip -d ServicesManagment && chmod +x ServicesManagment/Managment
    ```
  * Run Management by specifying the [required command line arguments](#required-command-line-arguments)
    ```bash
    cd ServicesManagment && ./Managment
    ```

### Required command line arguments
* `-i`, `--install`  Install mode. Download and install services to temp dirrectory
* `-r`, `--run`  Run mode. Launches previously installed to temp dirrectory services
* `-u`, `--update` Update mode. Update installed to temp dirrectory services

