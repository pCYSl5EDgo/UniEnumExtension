# UniEnumExtension

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

---

<details><summary>Performance Test code - [Flags]Enum</summary>

```cs
[Flags]
public enum FastFlagEnum : long
{
    z = 0,
    a = 1,
    b = 2,
    c = 4,
    d = 8,
    e = 0x10,
    f = 0x20,
    g = 0x40,
    h = 0x80,
}
```

```cs
[Flags]
public enum SlowFlagEnum : long
{
    z = 0,
    a = 1,
    b = 2,
    c = 4,
    d = 8,
    e = 0x10,
    f = 0x20,
    g = 0x40,
    h = 0x80,
}
```

```cs
public sealed class FlagsTestScript : MonoBehaviour
{
    public Text text;
    public Button button;
    private Stopwatch watch;
    void Start()
    {
        watch = new Stopwatch();
        button.onClick.AddListener(Calc);
    }

    private void Calc()
    {
        const long width = 256L;
        string a;
        watch.Restart();
        for (var i = 0; i < 100000; i++)
        {
            for (var j = 0L; j < width; j++)
            {
                a = ((SlowFlagEnum)j).ToString();
            }
        }
        watch.Stop();
        text.text = "SlowFlagEnum (0-" + width + ") : " + watch.ElapsedMilliseconds;
        watch.Restart();
        for (var i = 0; i < 100000; i++)
        {
            for (var j = 0L; j < width; j++)
            {
                a = ((FastFlagEnum)j).ToString();
            }
        }
        watch.Stop();
        text.text += "\nFastFlagEnum (0-" + width + ") : " + watch.ElapsedMilliseconds;
    }
}
```
</details>


**[Flags]Enumに対して2560万回ToString()するのに掛かった時間**

環境 | 通常のEnum (25600000times) | UniEnumExtensionで処理したEnum (25600000times)
----|----|----
Editor|54624ms|374ms
Windows 64bit Mono .net standard 2.0 Release Build|26146ms|52ms
Windows 64bit IL2CPP .net standard 2.0 Release Build|18735ms|83ms

---

<details><summary>Performance Test code - Continuous Enum starting from 0</summary>

```cs
public enum FastEnum : long
{
    a0 = 0x00
    , a1 = 0x10
    , a2 = 0x20
    , a3 = 0x30
    , a4 = 0x40
    , a5 = 0x50
    , a6 = 0x60
    , a7 = 0x70
    , a8 = 0x80
    , a9 = 0x90
    , aa = 0xa0
    , ab = 0xb0
    , ac = 0xc0
    , ad = 0xd0
    , ae = 0xe0
    , af = 0xf0,
    a01 = 0x01,
    a11 = 0x11,
    a21 = 0x21,
    a31 = 0x31,
    a41 = 0x41,
    a51 = 0x51,
    a61 = 0x61,
    a71 = 0x71,
    a81 = 0x81,
    a91 = 0x91,
    aa1 = 0xa1,
    ab1 = 0xb1,
    ac1 = 0xc1,
    ad1 = 0xd1,
    ae1 = 0xe1,
    af1 = 0xf1,
    a02 = 0x02,
    a12 = 0x12,
    a22 = 0x22,
    a32 = 0x32,
    a42 = 0x42,
    a52 = 0x52,
    a62 = 0x62,
    a72 = 0x72,
    a82 = 0x82,
    a92 = 0x92,
    aa2 = 0xa2,
    ab2 = 0xb2,
    ac2 = 0xc2,
    ad2 = 0xd2,
    ae2 = 0xe2,
    af2 = 0xf2
    , a03 = 0x03
    , a13 = 0x13
    , a23 = 0x23
    , a33 = 0x33
    , a43 = 0x43
    , a53 = 0x53
    , a63 = 0x63
    , a73 = 0x73
    , a83 = 0x83
    , a93 = 0x93
    , aa3 = 0xa3
    , ab3 = 0xb3
    , ac3 = 0xc3
    , ad3 = 0xd3
    , ae3 = 0xe3
    , af3 = 0xf3
    , a04 = 0x04
    , a14 = 0x14
    , a24 = 0x24
    , a34 = 0x34
    , a44 = 0x44
    , a54 = 0x54
    , a64 = 0x64
    , a74 = 0x74
    , a84 = 0x84
    , a94 = 0x94
    , aa4 = 0xa4
    , ab4 = 0xb4
    , ac4 = 0xc4
    , ad4 = 0xd4
    , ae4 = 0xe4
    , af4 = 0xf4
    , a05   = 0x05
    , a15   = 0x15
    , a25   = 0x25
    , a35   = 0x35
    , a45   = 0x45
    , a55   = 0x55
    , a65   = 0x65
    , a75   = 0x75
    , a85   = 0x85
    , a95   = 0x95
    , aa5   = 0xa5
    , ab5   = 0xb5
    , ac5   = 0xc5
    , ad5   = 0xd5
    , ae5   = 0xe5
    , af5   = 0xf5
    , a06   = 0x06
    , a16   = 0x16
    , a26   = 0x26
    , a36   = 0x36
    , a46   = 0x46
    , a56   = 0x56
    , a66   = 0x66
    , a76   = 0x76
    , a86   = 0x86
    , a96   = 0x96
    , aa6   = 0xa6
    , ab6   = 0xb6
    , ac6   = 0xc6
    , ad6   = 0xd6
    , ae6   = 0xe6
    , af6   = 0xf6
    , a07   = 0x07
    , a17   = 0x17
    , a27   = 0x27
    , a37   = 0x37
    , a47   = 0x47
    , a57   = 0x57
    , a67   = 0x67
    , a77   = 0x77
    , a87   = 0x87
    , a97   = 0x97
    , aa7   = 0xa7
    , ab7   = 0xb7
    , ac7   = 0xc7
    , ad7   = 0xd7
    , ae7   = 0xe7
    , af7   = 0xf7
    , a08   = 0x08
    , a18   = 0x18
    , a28   = 0x28
    , a38   = 0x38
    , a48   = 0x48
    , a58   = 0x58
    , a68   = 0x68
    , a78   = 0x78
    , a88   = 0x88
    , a98   = 0x98
    , aa8   = 0xa8
    , ab8   = 0xb8
    , ac8   = 0xc8
    , ad8   = 0xd8
    , ae8   = 0xe8
    , af8   = 0xf8
    , a09   = 0x09
    , a19   = 0x19
    , a29   = 0x29
    , a39   = 0x39
    , a49   = 0x49
    , a59   = 0x59
    , a69   = 0x69
    , a79   = 0x79
    , a89   = 0x89
    , a99   = 0x99
    , aa9   = 0xa9
    , ab9   = 0xb9
    , ac9   = 0xc9
    , ad9   = 0xd9
    , ae9   = 0xe9
    , af9   = 0xf9
    , a0a   = 0x0a
    , a1a   = 0x1a
    , a2a   = 0x2a
    , a3a   = 0x3a
    , a4a   = 0x4a
    , a5a   = 0x5a
    , a6a   = 0x6a
    , a7a   = 0x7a
    , a8a   = 0x8a
    , a9a   = 0x9a
    , aaa   = 0xaa
    , aba   = 0xba
    , aca   = 0xca
    , ada   = 0xda
    , aea   = 0xea
    , afa   = 0xfa
    , a0b   = 0x0b
    , a1b   = 0x1b
    , a2b   = 0x2b
    , a3b   = 0x3b
    , a4b   = 0x4b
    , a5b   = 0x5b
    , a6b   = 0x6b
    , a7b   = 0x7b
    , a8b   = 0x8b
    , a9b   = 0x9b
    , aab   = 0xab
    , abb   = 0xbb
    , acb   = 0xcb
    , adb   = 0xdb
    , aeb   = 0xeb
    , afb   = 0xfb
    , a0c   = 0x0c
    , a1c   = 0x1c
    , a2c   = 0x2c
    , a3c   = 0x3c
    , a4c   = 0x4c
    , a5c   = 0x5c
    , a6c   = 0x6c
    , a7c   = 0x7c
    , a8c   = 0x8c
    , a9c   = 0x9c
    , aac   = 0xac
    , abc   = 0xbc
    , acc   = 0xcc
    , adc   = 0xdc
    , aec   = 0xec
    , afc   = 0xfc
    , a0d   = 0x0d
    , a1d   = 0x1d
    , a2d   = 0x2d
    , a3d   = 0x3d
    , a4d   = 0x4d
    , a5d   = 0x5d
    , a6d   = 0x6d
    , a7d   = 0x7d
    , a8d   = 0x8d
    , a9d   = 0x9d
    , aad   = 0xad
    , abd   = 0xbd
    , acd   = 0xcd
    , add   = 0xdd
    , aed   = 0xed
    , afd   = 0xfd
    , a0e   = 0x0e
    , a1e   = 0x1e
    , a2e   = 0x2e
    , a3e   = 0x3e
    , a4e   = 0x4e
    , a5e   = 0x5e
    , a6e   = 0x6e
    , a7e   = 0x7e
    , a8e   = 0x8e
    , a9e   = 0x9e
    , aae   = 0xae
    , abe   = 0xbe
    , ace   = 0xce
    , ade   = 0xde
    , aee   = 0xee
    , afe   = 0xfe
    , a0f   = 0x0f
    , a1f   = 0x1f
    , a2f   = 0x2f
    , a3f   = 0x3f
    , a4f   = 0x4f
    , a5f   = 0x5f
    , a6f   = 0x6f
    , a7f   = 0x7f
    , a8f   = 0x8f
    , a9f   = 0x9f
    , aaf   = 0xaf
    , abf   = 0xbf
    , acf   = 0xcf
    , adf   = 0xdf
    , aef   = 0xef
    , aff   = 0xff
}
```

```cs
public enum SlowEnum : long
{
    a0 = 0x00
    , a1 = 0x10
    , a2 = 0x20
    , a3 = 0x30
    , a4 = 0x40
    , a5 = 0x50
    , a6 = 0x60
    , a7 = 0x70
    , a8 = 0x80
    , a9 = 0x90
    , aa = 0xa0
    , ab = 0xb0
    , ac = 0xc0
    , ad = 0xd0
    , ae = 0xe0
    , af = 0xf0,
    a01 = 0x01,
    a11 = 0x11,
    a21 = 0x21,
    a31 = 0x31,
    a41 = 0x41,
    a51 = 0x51,
    a61 = 0x61,
    a71 = 0x71,
    a81 = 0x81,
    a91 = 0x91,
    aa1 = 0xa1,
    ab1 = 0xb1,
    ac1 = 0xc1,
    ad1 = 0xd1,
    ae1 = 0xe1,
    af1 = 0xf1,
    a02 = 0x02,
    a12 = 0x12,
    a22 = 0x22,
    a32 = 0x32,
    a42 = 0x42,
    a52 = 0x52,
    a62 = 0x62,
    a72 = 0x72,
    a82 = 0x82,
    a92 = 0x92,
    aa2 = 0xa2,
    ab2 = 0xb2,
    ac2 = 0xc2,
    ad2 = 0xd2,
    ae2 = 0xe2,
    af2 = 0xf2
    , a03 = 0x03
    , a13 = 0x13
    , a23 = 0x23
    , a33 = 0x33
    , a43 = 0x43
    , a53 = 0x53
    , a63 = 0x63
    , a73 = 0x73
    , a83 = 0x83
    , a93 = 0x93
    , aa3 = 0xa3
    , ab3 = 0xb3
    , ac3 = 0xc3
    , ad3 = 0xd3
    , ae3 = 0xe3
    , af3 = 0xf3
    , a04 = 0x04
    , a14 = 0x14
    , a24 = 0x24
    , a34 = 0x34
    , a44 = 0x44
    , a54 = 0x54
    , a64 = 0x64
    , a74 = 0x74
    , a84 = 0x84
    , a94 = 0x94
    , aa4 = 0xa4
    , ab4 = 0xb4
    , ac4 = 0xc4
    , ad4 = 0xd4
    , ae4 = 0xe4
    , af4 = 0xf4
    , a05   = 0x05
    , a15   = 0x15
    , a25   = 0x25
    , a35   = 0x35
    , a45   = 0x45
    , a55   = 0x55
    , a65   = 0x65
    , a75   = 0x75
    , a85   = 0x85
    , a95   = 0x95
    , aa5   = 0xa5
    , ab5   = 0xb5
    , ac5   = 0xc5
    , ad5   = 0xd5
    , ae5   = 0xe5
    , af5   = 0xf5
    , a06   = 0x06
    , a16   = 0x16
    , a26   = 0x26
    , a36   = 0x36
    , a46   = 0x46
    , a56   = 0x56
    , a66   = 0x66
    , a76   = 0x76
    , a86   = 0x86
    , a96   = 0x96
    , aa6   = 0xa6
    , ab6   = 0xb6
    , ac6   = 0xc6
    , ad6   = 0xd6
    , ae6   = 0xe6
    , af6   = 0xf6
    , a07   = 0x07
    , a17   = 0x17
    , a27   = 0x27
    , a37   = 0x37
    , a47   = 0x47
    , a57   = 0x57
    , a67   = 0x67
    , a77   = 0x77
    , a87   = 0x87
    , a97   = 0x97
    , aa7   = 0xa7
    , ab7   = 0xb7
    , ac7   = 0xc7
    , ad7   = 0xd7
    , ae7   = 0xe7
    , af7   = 0xf7
    , a08   = 0x08
    , a18   = 0x18
    , a28   = 0x28
    , a38   = 0x38
    , a48   = 0x48
    , a58   = 0x58
    , a68   = 0x68
    , a78   = 0x78
    , a88   = 0x88
    , a98   = 0x98
    , aa8   = 0xa8
    , ab8   = 0xb8
    , ac8   = 0xc8
    , ad8   = 0xd8
    , ae8   = 0xe8
    , af8   = 0xf8
    , a09   = 0x09
    , a19   = 0x19
    , a29   = 0x29
    , a39   = 0x39
    , a49   = 0x49
    , a59   = 0x59
    , a69   = 0x69
    , a79   = 0x79
    , a89   = 0x89
    , a99   = 0x99
    , aa9   = 0xa9
    , ab9   = 0xb9
    , ac9   = 0xc9
    , ad9   = 0xd9
    , ae9   = 0xe9
    , af9   = 0xf9
    , a0a   = 0x0a
    , a1a   = 0x1a
    , a2a   = 0x2a
    , a3a   = 0x3a
    , a4a   = 0x4a
    , a5a   = 0x5a
    , a6a   = 0x6a
    , a7a   = 0x7a
    , a8a   = 0x8a
    , a9a   = 0x9a
    , aaa   = 0xaa
    , aba   = 0xba
    , aca   = 0xca
    , ada   = 0xda
    , aea   = 0xea
    , afa   = 0xfa
    , a0b   = 0x0b
    , a1b   = 0x1b
    , a2b   = 0x2b
    , a3b   = 0x3b
    , a4b   = 0x4b
    , a5b   = 0x5b
    , a6b   = 0x6b
    , a7b   = 0x7b
    , a8b   = 0x8b
    , a9b   = 0x9b
    , aab   = 0xab
    , abb   = 0xbb
    , acb   = 0xcb
    , adb   = 0xdb
    , aeb   = 0xeb
    , afb   = 0xfb
    , a0c   = 0x0c
    , a1c   = 0x1c
    , a2c   = 0x2c
    , a3c   = 0x3c
    , a4c   = 0x4c
    , a5c   = 0x5c
    , a6c   = 0x6c
    , a7c   = 0x7c
    , a8c   = 0x8c
    , a9c   = 0x9c
    , aac   = 0xac
    , abc   = 0xbc
    , acc   = 0xcc
    , adc   = 0xdc
    , aec   = 0xec
    , afc   = 0xfc
    , a0d   = 0x0d
    , a1d   = 0x1d
    , a2d   = 0x2d
    , a3d   = 0x3d
    , a4d   = 0x4d
    , a5d   = 0x5d
    , a6d   = 0x6d
    , a7d   = 0x7d
    , a8d   = 0x8d
    , a9d   = 0x9d
    , aad   = 0xad
    , abd   = 0xbd
    , acd   = 0xcd
    , add   = 0xdd
    , aed   = 0xed
    , afd   = 0xfd
    , a0e   = 0x0e
    , a1e   = 0x1e
    , a2e   = 0x2e
    , a3e   = 0x3e
    , a4e   = 0x4e
    , a5e   = 0x5e
    , a6e   = 0x6e
    , a7e   = 0x7e
    , a8e   = 0x8e
    , a9e   = 0x9e
    , aae   = 0xae
    , abe   = 0xbe
    , ace   = 0xce
    , ade   = 0xde
    , aee   = 0xee
    , afe   = 0xfe
    , a0f   = 0x0f
    , a1f   = 0x1f
    , a2f   = 0x2f
    , a3f   = 0x3f
    , a4f   = 0x4f
    , a5f   = 0x5f
    , a6f   = 0x6f
    , a7f   = 0x7f
    , a8f   = 0x8f
    , a9f   = 0x9f
    , aaf   = 0xaf
    , abf   = 0xbf
    , acf   = 0xcf
    , adf   = 0xdf
    , aef   = 0xef
    , aff   = 0xff
}
```

```cs
public sealed class NoFlagTestScript : MonoBehaviour
{
    public Text text;
    public Button button;
    private Stopwatch watch;
    void Start()
    {
        watch = new Stopwatch();
        button.onClick.AddListener(Calc);
    }

    private void Calc()
    {
        const long width = 256L;
        string a;
        watch.Restart();
        for (var i = 0; i < 100000; i++)
        {
            for (var j = 0L; j < width; j++)
            {
                a = ((SlowEnum)j).ToString();
            }
        }
        watch.Stop();
        text.text = "SlowEnum (0-" + width + ") : " + watch.ElapsedMilliseconds;
        watch.Restart();
        for (var i = 0; i < 100000; i++)
        {
            for (var j = 0L; j < width; j++)
            {
                a = ((FastEnum)j).ToString();
            }
        }
        watch.Stop();
        text.text += "\nFastEnum (0-" + width + ") : " + watch.ElapsedMilliseconds;
    }
}
```
</details>

**0から始まる連続したEnumに対して2560万回ToString()するのに掛かった時間**

環境 | 通常のEnum (25600000times) | UniEnumExtensionで処理したEnum (25600000times)
----|----|----
Editor|31791ms|394ms
Windows 64bit Mono .net standard 2.0 Release Build|16264ms|46ms
Windows 64bit IL2CPP .net standard 2.0 Release Build|17011ms|56ms