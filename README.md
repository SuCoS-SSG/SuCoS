# SuCoS

<img src="SuCoS-logo.svg" width="512px" style="display: block;margin-left: auto;margin-right: auto;" />

🎉 Welcome to **SuCoS** (**J**ui**C**e**S**, in Brazilian Portuguese), the one of the fastest Static Site Generator out there! 🚀

> **DISCLAIMER**: **SuCoS** is in a **ALPHA state**! Please do not use for ANY real site for now. Prepare to be entertained by unexpected behaviors! 🎢

Official site: https://sucos.brunomassa.com

[![Latest release](https://gitlab.com/sucos/sucos/-/badges/release.svg)](https://gitlab.com/sucos/sucos)
![Pipepline](https://gitlab.com/sucos/sucos/badges/main/pipeline.svg?ignore_skipped=true)
[![Latest release](https://gitlab.com/sucos/sucos/badges/main/coverage.svg)](https://gitlab.com/sucos/sucos)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/1fe0cc1ca72649ee9b85e13e7294a03a)](https://app.codacy.com/gl/sucos/sucos/dashboard?utm_source=gl&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)

## Install

All the [Releases](https://gitlab.com/sucos/sucos/-/releases) for Linux, Windows are a single executable! Just download and use.

## Usage

First, navigate to the **SuCoS** folder, then, run the following command:

```sh
SuCoS <YOUR_SITE_PATH>
```

```sh
SuCoS.exe <YOUR_SITE_PATH>
```

if `<YOUR_SITE_PATH>` not present, it will default to current folder.

Watch in awe as **SuCoS** creates a static site from your input files, or as it possibly implodes (with a 50/50 chance).

If all goes well (fingers crossed 🤞), you'll have a shiny new static website in the `public` folder.

## Build Requirements

**SuCoS** is built with **DotNet 8**, and the latest C# speed and features!

```sh
git clone https://gitlab.com/sucos/sucos.git # or git@gitlab.com:sucos/sucos.git
cd SuCoS
dotnet build # or `build.sh clean restore compile`
```

## License

This piece of software is brought to you under the [MIT license](LICENSE), because we believe in the power of sharing (and bug fixing together).

## Contributing

We welcome contributions in the form of bug reports, feature requests, and pull requests. Just remember, we're here for the fun, so keep it light-hearted and enjoyable! [Check our guidelines](CONTRIBUTING.md).

## Final Words

Remember, **SuCoS** is a work in progress, and it's important to maintain calm while using this software. Have fun, and may the 4th be with you! 😄

Created by [Bruno MASSA](https://www.brunomassa.com) and hopefully your contributions too!
