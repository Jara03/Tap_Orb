# Firebase Analytics / Crashlytics (Unity)

## Setup

1. **Importer le Firebase Unity SDK** (Analytics + Crashlytics si besoin).
   - Téléchargez le SDK Unity depuis la console Firebase ou le site officiel.
   - Importez les packages :
     - `FirebaseAnalytics.unitypackage`
     - `FirebaseCrashlytics.unitypackage` (optionnel)
2. **External Dependency Manager (EDM4U)**
   - Le projet contient déjà l’EDM4U. Après import, exécuter :
     - **Assets > External Dependency Manager > Android Resolver > Resolve**
     - **Assets > External Dependency Manager > iOS Resolver > Resolve** (si iOS)
3. **Ajouter les fichiers de config** (TODO : ne pas versionner de faux fichiers).
   - Android : placez `google-services.json` dans `Assets/`.
   - iOS : placez `GoogleService-Info.plist` dans `Assets/`.
   - Le wrapper `Track` logge un warning si ces fichiers sont absents.
4. **Activer le tracking**
   - Ajoutez le scripting define symbol `TRACKING_ENABLED` (Project Settings > Player > Scripting Define Symbols).
   - Pour désactiver rapidement : retirez le symbol ou mettez `Track.RuntimeEnabled = false`.

## How to test

### DebugView (recommandé)

**Android**
1. Branchez un device et activez DebugView :
   - `adb shell setprop debug.firebase.analytics.app <your.package.name>`
2. Lancez le build (dev build conseillé) et déclenchez un event (niveau, ads, etc.).
3. Vérifiez dans Firebase Console > Analytics > DebugView.
4. Pour désactiver DebugView :
   - `adb shell setprop debug.firebase.analytics.app .none.`

**iOS**
1. Dans Xcode, ajoutez l’argument de lancement : `-FIRDebugEnabled`.
2. Lancez le build et déclenchez un event.
3. Vérifiez dans Firebase Console > Analytics > DebugView.

### Vérifier dans l’éditeur / device
- En **Development Build** ou dans l’éditeur, le wrapper logge les events dans la console avec le préfixe `[Track]`.
- Exemple attendu : `[Track] level_start level=3`.

## Notes
- Les noms d’events sont normalisés en `snake_case`.
- Les paramètres sont limités (max 10) et filtrés pour éviter d’envoyer des données personnelles.
