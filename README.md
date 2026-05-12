# ☕ DCafe

A mobile coffee shop ordering app built with **.NET MAUI**, designed for a smooth and elegant café experience on Android and iOS.

Download APK here:
https://drive.google.com/file/d/1ViKwonI3NL6K-aIx15HaxpKlCxli33WD/view?usp=sharing
---

## 📱 Screenshots

| Splash Screen | Home / Menu | Product Detail |
|:---:|:---:|:---:|
| ![Splash](src/Screenshots/splash.png) | ![Menu](src/Screenshots/menu.png) | ![Detail](src/Screenshots/productdetail.png) |

---

## ✨ Features

- **Browse Categories** — Filter drinks by Coffee, Cold Brew, or Non-Coffee
- **Featured Products** — Discover highlighted menu items at a glance
- **Product Detail Page** — View descriptions, categories, ratings, and related items
- **Add to Cart** — Seamless cart management
- **Favorites** — Save your go-to drinks
- **Search** — Quickly find drinks and more
- **Account Management** — Personalized user account

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Framework | .NET MAUI |
| Language | C# |
| Architecture | MVVM (Model-View-ViewModel) |
| Backend / Data | Firebase (Realtime Database) |
| UI | XAML |

---

## 🗂️ Project Structure

```
DCafe/
├── src/
│   ├── Converters/        # Value converters for data binding
│   ├── Models/            # Data models (Product, Category, etc.)
│   ├── Platforms/         # Platform-specific configurations
│   ├── Properties/        # Assembly properties
│   ├── Resources/         # Images, fonts, and styles
│   ├── Services/          # Firebase and data services
│   ├── ViewModels/        # MVVM ViewModels
│   ├── Views/             # XAML pages and UI
│   ├── App.xaml           # App-level resources
│   ├── AppShell.xaml      # Shell navigation
│   ├── MainPage.xaml      # Entry page
│   ├── MauiProgram.cs     # App startup & DI configuration
│   └── MauiStoreApp.csproj
├── firebase_products.json # Firebase product seed data
├── global.json
├── DCafe.sln
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **MAUI** workload installed
- A Firebase project with Realtime Database enabled

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-username/DCafe.git
   cd DCafe
   ```

2. **Configure Firebase**
   - Create a Firebase project at [console.firebase.google.com](https://console.firebase.google.com)
   - Download your `google-services.json` (Android) and/or `GoogleService-Info.plist` (iOS)
   - Place them in the appropriate `Platforms/` folder
   - Update `firebase_products.json` with your product data or import it into your Firebase Realtime Database

3. **Restore dependencies and run**
   ```bash
   dotnet restore
   dotnet build
   ```
   Or open `DCafe.sln` in Visual Studio and press **F5** to run.

---

## 📋 Menu Items (Sample)

| Product | Category | Price |
|---|---|---|
| Espresso | Coffee | ₱90.00 |
| Americano | Coffee | ₱110.00 |
| Cappuccino | Coffee | ₱120.00 |

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines on how to get started.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Commit your changes (`git commit -m 'Add some feature'`)
4. Push to the branch (`git push origin feature/your-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the terms found in [LICENSE](./LICENSE).

---

> Made with ☕ and .NET MAUI
