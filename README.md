A tiny cross platform tool to filter out duplicate files within specified directory. 

### Install

You can go directly download from the [Release](https://github.com/JerryBian/dff/releases) page according to your target platform.

Or you can use dotnet [global tools](https://www.nuget.org/packages/dff/) if you have already [.NET 6](https://dotnet.microsoft.com/download) installed.

```sh
dotnet tool install --global dff
```
For Mac users with zsh, please manually add the dotnet global tool path to `~/.zshrc`. Simply add this line as descriped in this [issue](https://github.com/dotnet/sdk/issues/9415#issuecomment-406915716).

```sh
export PATH=$HOME/.dotnet/tools:$PATH
```

If you would like to upgrade to latest version as you already installed, you can:

```sh
dotnet tool update --global dff
```

### Usage

`dff [options] [dir]`

Supported options are:

```sh
-r, --recursive    Include sub directories. Default to false.

-v, --verbose      Display detailed logs. Default to false.

-e, --export       Export all duplicate paths. Default to false.

--help             Display this help screen.

--version          Display version information.

dir (pos. 0)       The target folders(can be specified multiple). Default to current folder.
```

After the command completed, you can also go to the log file besides the terminal output.

### License

MIT