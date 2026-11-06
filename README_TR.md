# İstanbul 3D (Unity, mobil) - Kurulum

Bu proje bilgisayarsız, GitHub Actions ile APK derlemek için hazırlandı.
Sahne, animator ve materyaller derleme sırasında kodla üretilir (Assets/Editor/BuildScript.cs).

## 1) Repoya yükle (Termux)
    pkg install git unzip
    unzip IstanbulGame.zip && cd IstanbulGame
    git init && git branch -M main
    git add . && git commit -m "ilk surum"
    git remote add origin https://github.com/KULLANICI/REPO.git
    git push -u origin main
(GitHub'da boş bir repo oluştur. Şifre yerine "Personal Access Token" kullan.)

## 2) Unity lisansı (bir kere)
1. GitHub > Actions > "1 - Lisans dosyasi al" > Run workflow. Biten işten `Unity-Aktivasyon-Dosyasi` (.alf) indir.
2. license.unity3d.com/manual adresinde .alf yükle, Personal seç, .ulf dosyasını indir.
3. Repo > Settings > Secrets and variables > Actions: `UNITY_LICENSE` = .ulf içeriğinin tamamı,
   `UNITY_EMAIL` ve `UNITY_PASSWORD` = Unity hesabın.
Unity bu süreci zaman zaman değiştiriyor; olmazsa game.ci/docs/github/activation sayfasına bak.

## 3) APK derle
Actions > "2 - APK derle" > Run workflow. İş bitince "IstanbulGame-APK" artifact'ini indir, telefona kur.
İlk derleme 30-60 dk sürebilir. Hata olursa log'u bana yapıştır.

## Kontroller
- Sol: yüzen joystick. Sonuna kadar itersen koşar (sprint), ortada yürür/tırısa kalkar.
- Zıpla düğmesi sol tarafta (koordinat: MobileControls.cs, jr.anchoredPosition).
- Sağ yarıda sürükle: kamera. Sol üstte mini harita, üstte semt adı.
- Suya girersen yüzer.
