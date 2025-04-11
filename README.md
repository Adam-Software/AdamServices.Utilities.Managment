# AdamServices.Utilities.Managment
[![.NET Build And Publish Release](https://github.com/Adam-Software/AdamServices.Utilities.Managment/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Adam-Software/AdamServices.Utilities.Managment/actions/workflows/dotnet.yml)   
![GitHub License](https://img.shields.io/github/license/Adam-Software/AdamServices.Utilities.Managment)
![GitHub Release](https://img.shields.io/github/v/release/Adam-Software/AdamServices.Utilities.Managment)

A utility for downloading, running, and updating [`adamservices-services`](https://github.com/topics/adamservices-services) projects.

Use the shared [wiki](https://github.com/Adam-Software/AdamServices.Utilities.Managment/wiki) to find information about the project.

## For users
### Permanent links to releases
* **Windows [x64]**
  ```
  https://github.com/Adam-Software/AdamServices.Utilities.Managment/releases/latest/download/Managment.win64.portable.zip
  ```
* **Linux [arm64]**
  ```
  https://github.com/Adam-Software/AdamServices.Utilities.Managment/releases/latest/download/Managment.arm64.portable.zip
  ```
### Install
* **Windows [x64]**
  * Download using the [permalink](#permanent-links-to-releases)
  * Unzip and run Managmet.exe by specifying the [required command line arguments](#required-command-line-arguments)

* **Linux [arm64]**
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
    cd ServicesManagment && ./Managment [-i -r -u]
    ```

### Required command line arguments
* `-i`, `--install`  Install mode. Download and install services to temp dirrectory
* `-r`, `--run`  Run mode. Launches previously installed to temp dirrectory services
* `-u`, `--update` Update mode. Update installed to temp dirrectory services

### Required .NET version
The program uses a dotnet tool such as `dotnet publish`, `dotnet exec` to build and run service projects.
Required version **DotNet 8**

### Install 8 .NET version
* **Windows [x64]**   
  Download and install SDK uses this [link](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
  
* **Linux[arm64]**   
  Raspberry Pi 3B+/Raspberry Pi Zero 2W/Raspberry Pi 4 and/Raspberry Pi 5    
  You can install Dot Net 8 on the Raspberry Pi in one command by executing;
  ```bash
  wget -O - https://raw.githubusercontent.com/pjgpetecodes/dotnet8pi/main/install.sh | sudo bash
  ```


## Thanks
[@pjgpetecodes](https://github.com/pjgpetecodes) for https://github.com/pjgpetecodes/dotnet8pi

