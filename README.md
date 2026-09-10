[![.NET](https://img.shields.io/badge/.NET-9-512BD4)](https://dotnet.microsoft.com/)
[![Paint.NET](https://img.shields.io/badge/Paint.NET-5.1%2B-5C2D91)](https://www.getpaint.net/)
[![License](https://img.shields.io/github/license/daniilkorochansky/magnetic-lasso)](https://github.com/daniilkorochansky/magnetic-lasso/blob/main/LICENSE)
[![Release](https://img.shields.io/github/v/release/daniilkorochansky/magnetic-lasso)](https://github.com/daniilkorochansky/magnetic-lasso/releases)

# Magnetic Lasso
<img width="128" height="128" alt="magnetic_lasso_readme" src="https://github.com/user-attachments/assets/29b881bd-6c19-4dbe-a7de-7217017bd78d" />

Magnetic Lasso tool for Paint.NET using OpenCV Intelligent Scissors.

Quickly create precise selections by snapping the contour to image edges, with real-time magnetic previews and editable contour segments.

## Overview
<img width="826" height="594" alt="image" src="https://github.com/user-attachments/assets/df03f957-c0ec-4b66-9029-5d3363c71b1f" />

<img width="826" height="505" alt="copy_magnetic_lasso" src="https://github.com/user-attachments/assets/5374f747-5980-4474-9c50-4d5ed22e8964" />

## Key Features
* **Smart Edge Snapping:** Automatically detects and snaps to object boundaries using a fluid livewire/intelligent scissors algorithm.
* **Manual Anchor Points:** Left-click anywhere to manually force an anchor point, giving you full control over low-contrast areas or sharp corners.
* **On-the-Fly Correction:** Easily undo mistakes or completely reset the selection path using standard hotkeys.
* **One-Click Export with Transparency:** Automatically copies the final isolated object to your clipboard with full alpha-channel (transparency) preservation.

## Installation
* Download the latest release.
* Unzip the archive into the [Paint.Net folder]/Effects.

## Development
### Requirements
- .NET 9 SDK
- Paint.NET 5.1 or later
- Visual Studio 2026 or another compatible .NET IDE

### Clone the repository
```bash
git clone https://github.com/daniilkorochansky/magnetic-lasso.git
cd magnetic-lasso
```

### Build
Restore NuGet packages and build the project in Release configuration:
```bash
dotnet restore
dotnet build -c Release
```
