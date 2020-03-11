A tiny cross platform tool to filter out duplicate files within specified directory. 

### Installation

Make sure you have latest [.NET Core SDK](https://dotnet.microsoft.com/download) installed. It requires .NET Core 3.1 or newer.

#### Install

```sh
dotnet tool install --global DuplicateFileFinder
```
For Mac users with zsh, please manually add the dotnet global tool path to `~/.zshrc`. Simply add this line as descriped in this [issue](https://github.com/dotnet/sdk/issues/9415#issuecomment-406915716).

```sh
export PATH=$HOME/.dotnet/tools:$PATH
```

#### Update

If you would like to upgrade to latest version as you already installed, you can:

```sh
dotnet tool update --global DuplicateFileFinder
```
### Usage

There is only one option `-i` or `--input` is provided to accept which directory you would like this tool to process. If you didn't specify this argument, it will be default to current working directory.

The command name is `dff`.

```sh
dff /var/test
```

While the process completed, all duplicate files would be moved to another directory. It will not be removed by this tool, so your data is always safe and there.

### License

[MIT](LICENSE).

