# UniEnumExtension
===
数倍から数百倍列挙型を高速化するライブラリ。

## 機能概要

列挙型のToString()は内部的に仮想メソッドであるEnum.ToString()が呼ばれることが知られています。
そしてそれはリフレクションを使用しており低速です。
特に[Flags]属性付きの列挙型においてToString()を行った場合、都度StringBuilderをインスタンス化しアロケーションを引き起こします。

[Enums.NET](https://github.com/TylerBrinkley/Enums.NET)のような優れた解決策が多くのC#プログラマによって提唱されてきました。
それは悪い解決策ではありませんが、しかし、特定のライブラリへのソースコードレベルでの依存を強要します。

UniEnumExtensionはあなたのソースコードに一切の変更を加えずとも、列挙型が優れた性能を持って振る舞うようにします。
Unity Playerをビルドする時に静的にIL生成を行い、あなたのDLLを適切に編集します。

## ライセンス

GNU General Public License version 3とプロプライエタリライセンスのデュアルライセンスです。
非GPLなライセンスをお求めの方は[Booth](https://pcysl5edgo.booth.pm/)からご購入ください。

## 必須環境
**Unity 2018.4**以上を対象としています。
https://unity3d.com/get-unity/download からダウンロードすることをおすすめします。

## インストール方法 その１
このリポジトリをあなたのUnityプロジェクトの**Package**以下にコピーします。

基本的にgit cloneを以下のようにcmdやターミナルから行えばよいでしょう。

```none
cd Packages
git clone https://github.com/pCYSl5EDgo/UniEnumExtension.git
```

## インストール方法 その２

`Packages/manifest.json`があるはずです。
そこに以下のように一行をdependencies内に追記してください。

```js
{
  "dependencies": {
    "unienumextension": "https://github.com/pCYSl5EDgo/UniEnumExtension.git",
    ...
  },
}
```

## 性能比較