# PCSX2 Upscaler

## Overview

PCSX2 Upscaler is a graphical application designed to upscale texture files for the PCSX2 emulator using the `waifu2x-caffe` tool. This application allows users to select input and output folders for textures, specify the `waifu2x-caffe` executable, and then process the textures to enhance their quality. The app provides real-time feedback on the progress, estimated time remaining, and estimated storage requirements.

## Core Features

- **Folder Selection**: Easily browse and select the folder containing the textures to be upscaled.
- **Output Folder Selection**: Choose the output folder where the upscaled textures will be saved.
- **`waifu2x-caffe` Path Selection**: Specify the path to the `waifu2x-caffe` executable.
- **Real-time Progress Updates**: Display progress, estimated time remaining, and estimated storage requirements.
- **Start and Stop**: Control the upscaling process with Start and Stop buttons.
- **Terminal Output**: Option to display terminal output for detailed processing logs.

## Prerequisites

- .NET 8 SDK
- AvaloniaUI
- `waifu2x-caffe`

## How to Build

1. **Clone the Repository**:
    ```sh
    git clone https://github.com/pnordboj/PlayStation2Upscaler.git
    cd PCSX2Upscaler
    ```

2. **Install .NET 8 SDK**: Make sure you have the .NET 8 SDK installed. You can download it from [here](https://dotnet.microsoft.com/download).

3. **Restore Dependencies**:
    ```sh
    dotnet restore
    ```

4. **Build the Project**:
    ```sh
    dotnet build
    ```

5. **Run the Application**:
    ```sh
    dotnet run
    ```

## How to Use

1. **Launch the Application**:
    Run the application from your build directory.

2. **Select Input Folder**:
    - Click on the `Browse` button next to the "Select the folder containing textures" field.
    - Choose the folder that contains the texture files you want to upscale.

3. **Select Output Folder**:
    - Click on the `Browse` button next to the "Set output folder" field.
    - Choose the folder where you want the upscaled textures to be saved.

4. **Select `waifu2x-caffe` Executable**:
    - Click on the `Browse` button next to the "Set waifu2x-caffe path" field.
    - Choose the `waifu2x-caffe` executable file (`waifu2x-caffe-cui.exe`).

5. **Start Upscaling**:
    - Once all the paths are set, the `Upscale Textures` button will be enabled.
    - Click `Upscale Textures` to start the upscaling process.
    - Monitor the progress through the progress bar and the status text.

6. **Pause/Resume Upscaling**:
    - Click `Resume` to resume the upscaling process.

7. **Stop Upscaling**:
    - Click `Stop` to stop the upscaling process at any time.

8. **View Terminal Output**:
    - Check the "Show Terminal" checkbox to display the terminal output.

## Contributing

We welcome contributions! If you find a bug or want to add a feature, feel free to open an issue or submit a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact

For any questions or suggestions, feel free to contact us at [patricknj.dev@gmail.com](mailto:patricknj.dev@gmail.com).
