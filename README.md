# UniEnumExtension
===
Blazingly Fast Enum Library.

[日本語](README-jp.md)

## Features

Enum's ToString() is known as a virtual method and slow one. 
[Flags]Enum's ToString() actually allocates StringBuilder instance and is very slow.

Many C# programmers reimplement Enum.ToString() such as [Enums.NET](https://github.com/TylerBrinkley/Enums.NET). 
It's a not bad solution but makes your solution depending on those libraries.

This library never forces your solution to depend on it.
It just rewrites your dlls so that your solution have much more eddifient and much less allocation Enum's APIs.

## Requirement
Install **Unity 2018.4** or above. You can download the latest Unity on https://unity3d.com/get-unity/download.

## UniEnumExtension Package Install(1)
Download the latest this repository.

Move the downloaded repository folder into the **Package** folder of your Unity project.

Generally, you can make it using a console (or terminal) application by just a few commands as below:

```none
cd Packages
git clone https://github.com/pCYSl5EDgo/UniEnumExtension.git
```

## UniEnumExtension Package Install(2)

Find `Packages/manifest.json` in your project and edit it to look like this:
```js
{
  "dependencies": {
    "unienumextension": "https://github.com/pCYSl5EDgo/UniEnumExtension.git",
    ...
  },
}
```