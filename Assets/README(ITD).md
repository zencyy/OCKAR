# Old Chang Kee AR App

**Diploma in Immersive Media Project** | **Ngee Ann Polytechnic**

A mobile Augmented Reality (AR) application designed to gamify the Old Chang Kee brand experience. Players scan the OCK logo to open a OCK Card Pack," collect OCK food items, fill their collection, and unlock real-world vouchers.

---      

## Platforms & Hardware Requirements

### **Supported Platforms**
* **Android:** Android 7.0 (Nougat) and higher (Requires ARCore support).
* **iOS:** iOS 11.0 and higher (Requires ARKit support).

### **Hardware Required**
* **Mobile Device:** Must have a working rear-facing camera.
* **Internet Connection:** Active Wi-Fi or 4G/5G connection is strictly required for Firebase Database synchronization and Authentication.
* **Image Target:** You must have the **Old Chang Kee logo** visible to play.
    * *Note:* You can scan the logo from a physical storefront signboard, a printed image, or a digital image displayed on a computer screen.

---

##  Instructions & Key Controls

### **How to Run**
1.  **Install:** Sideload the `.apk` file onto your Android device.
2.  **Login:** Launch the app and create an account (Sign Up) or Log In with an existing email/password.
3.  **Permissions:** Grant Camera and Location permissions when prompted.

### **Controls**
* **Navigation:** Use the bottom menu buttons to switch between **Scan**, **Collection**, **Rewards**, and **Profile**.
* **Scanning (AR Gameplay):**
    1.  **Point Camera:** Aim your device at the Old Chang Kee Logo.
    2.  **Tap the Pack:** When the 3D pack appears floating over the logo, **tap it once** to open it.
    3.  **Tap the Food:** A random food item will pop out. **Tap the food item** to collect it.
    4.  **Audio Cues:** Listen for the "open" and "collect" sound effects to confirm your actions.

### **Interface**
* **Collection Screen:** View your collected items. Tapping an icon shows the item's description and rarity.
* **Rewards Screen:** Track your progress toward vouchers. When a bar reaches 100%, the "Claim" button becomes active.
* **Vouchers Screen:** View your generated voucher codes and expiry dates.

---

## Game Mechanics ("Answer Key")

The objective is to collect duplicate items to reach a "Threshold." Once the threshold is met, a voucher is unlocked.

| Food Item | Rarity | Drop Rate | Quantity Needed | Reward |
| :--- | :--- | :--- | :--- | :--- |
| **Ngor Hiang** | Legendary | 5% | **3** | $20 Off Voucher |
| **Chicken Wing** | Rare | 10% | **5** | $15 Off Voucher |
| **Sotong Ball** | Uncommon | 15% | **8** | $10 Off Voucher |
| **Spring Roll** | Uncommon | 20%* | **8** | $10 Off Voucher |
| **Curry Puff** | Common | 40% | **10** | $5 Off Voucher |
| **Fish Ball** | Common | 30% | **10** | $5 Off Voucher |


---

## 🛠 Admin Panel ("Game Cheats & Hacks")

This project includes a **Web-Based Admin Dashboard** that functions as a "God Mode" tool. It allows administrators to manipulate game data in real-time without recompiling the app.

### **How to Access**
1.  Open the `admin/index.html` file in any modern web browser.
2.  Log in using the Administrator Credentials:
    * **Email:** `admin@ock.com`
    * **Password:** *(As set in Firebase Console)*

### **Cheats & Features**
1.  **Instant Win (Inventory Hack):**
    * Navigate to the **User Management** tab.
    * Click **Edit** next to your username.
    * Manually change your `Ngor Hiang` count to `3`.
    * *Result:* The $20 Voucher will instantly unlock in your app.
2.  **God Mode (Drop Rate Hack):**
    * Navigate to the **Game Items** tab.
    * Change the **Drop Rate** of `Ngor Hiang` from `5` to `100`.
    * *Result:* Every subsequent scan will guarantee a Legendary item.
3.  **Difficulty Nerf:**
    * In the **Game Items** tab, change the **Reward Threshold** for `Curry Puff` from `10` to `1`.
    * *Result:* You only need to find 1 Curry Puff to get the $5 voucher.

---

##  Limitations & Known Issues

1.  **Lighting Conditions:** The AR tracking may drift or fail in dimly lit environments or if there is heavy glare on the logo.
2.  **Audio Muting:** On some mobile devices (especially iPhones), game audio is muted if the physical "Silent Mode" switch is enabled. Please unmute your device.
3.  **Collection Lag:** Occasionally, there may be a 1-second delay between tapping a food item and the "Added to Collection" notification appearing. This is due to network latency when writing to the Firebase Realtime Database.
4.  **Occlusion:** The app does not currently support People/Object Occlusion. Virtual food items will appear to float "in front" of physical objects (like your hand) even if they are behind them.

---

##  References & Credits

### **Assets**
* **3D Models:**
    * *Curry Puff, Fishball, Sotong Ball:* all Food Items generated with TripoAI.
    * *Pack Design:* generated with TripoAI and Gemini.
* **2D Graphics:**
    * *Old Chang Kee Logo:* Property of Old Chang Kee (Used for educational assignment only).
    * *UI Icons:* FontAwesome & Custom Sprite Pack from Unity Assets.

### **Audio**
* **Tap Sound:** `coin-gold-box-open-14773.mp3` (Pixabay Royalty Free).
* **Success Sound:** `claim_success.mp3` (Pixabay Royalty Free).

### **Codes**
USed Gemini to help debug errors during the process.

### **Technology Stack**
* **Unity 2022.3 LTS:** Core Engine.
* **AR Foundation (ARCore/ARKit):** Image Tracking.
* **Firebase SDK:** Auth & Realtime Database.
* **Web Admin:** HTML5, CSS3, Vanilla JavaScript (ES6 Modules).

---

**Developed by Zen and Chun Yong**
*Last Updated: December 2025*
