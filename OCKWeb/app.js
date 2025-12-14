// Import Firebase functions from the CDN
import { initializeApp } from "https://www.gstatic.com/firebasejs/10.7.1/firebase-app.js";
import { getAuth, signInWithEmailAndPassword, signOut, onAuthStateChanged } from "https://www.gstatic.com/firebasejs/10.7.1/firebase-auth.js";
import { getDatabase, ref, onValue, update, remove } from "https://www.gstatic.com/firebasejs/10.7.1/firebase-database.js";
// --- CONFIGURATION ---
// 1. Go to Firebase Console > Project Settings > General > Your Apps > Web
// 2. Copy the config object and paste it below
const firebaseConfig = {
    apiKey: "AIzaSyC25G7Nyt3KZ1a9X9lYPIUenjRFeLtmKlg",
    authDomain: "ockar-71e31.firebaseapp.com",
    databaseURL: "https://ockar-71e31-default-rtdb.asia-southeast1.firebasedatabase.app", // Check your JSON file URL!
    projectId: "ockar-71e31",
    storageBucket: "ockar-71e31.firebasestorage.app",
    messagingSenderId: "996875423670",
    appId: "1:996875423670:web:be1ed7b229b0ed9d7dfb17"
};

// --- SECURITY SETTING ---
// REPLACE THIS WITH THE EXACT EMAIL YOU CREATED IN FIREBASE CONSOLE
const ADMIN_EMAIL = "zenadmin@abc.com"; 

// Initialize
const app = initializeApp(firebaseConfig);
const auth = getAuth(app);
const db = getDatabase(app);

// Global Variables to hold data locally for easier access
let globalItemsData = {};
let globalUsersData = {};

// --- AUTHENTICATION (Same as before) ---
window.login = () => {
    const email = document.getElementById('admin-email').value;
    const pass = document.getElementById('admin-password').value;
    const errorText = document.getElementById('login-error');
    errorText.textContent = "Verifying...";

    signInWithEmailAndPassword(auth, email, pass)
        .then((userCredential) => {
            if (userCredential.user.email === ADMIN_EMAIL) {
                document.getElementById('login-overlay').style.display = 'none';
                document.getElementById('dashboard-container').style.display = 'flex';
            } else {
                signOut(auth);
                throw new Error("Access Denied: Not Admin.");
            }
        })
        .catch((error) => errorText.textContent = error.message);
};

window.logout = () => signOut(auth).then(() => window.location.reload());

onAuthStateChanged(auth, (user) => {
    if (user && user.email === ADMIN_EMAIL) {
        document.getElementById('login-overlay').style.display = 'none';
        document.getElementById('dashboard-container').style.display = 'flex';
        initRealtimeListeners();
    } else {
        document.getElementById('login-overlay').style.display = 'flex';
        document.getElementById('dashboard-container').style.display = 'none';
    }
});

// --- REALTIME DATA ---
function initRealtimeListeners() {
    const usersRef = ref(db, 'users');
    onValue(usersRef, (snapshot) => {
        globalUsersData = snapshot.val() || {};
        renderStats(globalUsersData);
        renderUsersTable(globalUsersData);
    });

    const itemsRef = ref(db, 'game_data/items');
    onValue(itemsRef, (snapshot) => {
        globalItemsData = snapshot.val() || {};
        renderItems(globalItemsData);
    });
}

// --- RENDER FUNCTIONS ---
function renderStats(users) {
    // (Same stats logic as before)
    const userKeys = Object.keys(users);
    let totalScans = 0;
    let totalVouchers = 0;
    userKeys.forEach(key => {
        totalScans += (users[key].totalScans || 0);
        if (users[key].myVouchers) totalVouchers += Object.keys(users[key].myVouchers).length;
    });
    document.getElementById('total-users').innerText = userKeys.length;
    document.getElementById('total-scans').innerText = totalScans;
    document.getElementById('total-vouchers').innerText = totalVouchers;
}

function renderUsersTable(users) {
    const tbody = document.getElementById('users-table-body');
    tbody.innerHTML = ''; 

    Object.keys(users).forEach(userId => {
        const u = users[userId];
        const vouchersCount = u.myVouchers ? Object.keys(u.myVouchers).length : 0;
        
        tbody.innerHTML += `
            <tr>
                <td><strong>${u.username || "Unknown"}</strong></td>
                <td>${u.email || "N/A"}</td>
                <td>${u.totalScans || 0}</td>
                <td>${vouchersCount} Active</td>
                <td>
                    <button class="save-btn" style="background:#007bff;" onclick="openUserModal('${userId}')">
                        <i class="fas fa-edit"></i> Edit
                    </button>
                </td>
            </tr>
        `;
    });
}

function renderItems(items) {
    const grid = document.getElementById('items-grid');
    grid.innerHTML = '';

    Object.keys(items).forEach(key => {
        const item = items[key];
        // NEW: Added Reward Threshold Input
        grid.innerHTML += `
            <div class="item-card">
                <h3>${item.name}</h3>
                <p>Rarity: ${item.rarity}</p>
                <hr style="border:0; border-top:1px solid #eee; margin:10px 0;">
                
                <div style="display:flex; justify-content:space-between; margin-bottom:10px;">
                    <label>Drop Rate (%):</label>
                    <input type="number" id="rate-${key}" value="${item.dropRate}" style="width:50px;">
                </div>

                <div style="display:flex; justify-content:space-between; margin-bottom:10px;">
                    <label>Threshold:</label>
                    <input type="number" id="thresh-${key}" value="${item.rewardThreshold || 10}" style="width:50px;">
                </div>

                <button class="save-btn" onclick="updateItem('${key}')">Save</button>
            </div>
        `;
    });
}

// --- USER EDIT MODAL LOGIC ---

window.openUserModal = (userId) => {
    const user = globalUsersData[userId];
    if(!user) return;

    document.getElementById('user-modal').style.display = "block";
    document.getElementById('modal-username').innerText = "Edit: " + (user.username || userId);
    document.getElementById('modal-userid').value = userId;
    
    // 1. Set Total Scans
    document.getElementById('modal-scans').value = user.totalScans || 0;

    // 2. Build Collection Counts List
    // We loop through ALL game items, so we can see 0 for items they don't have
    const collectionDiv = document.getElementById('modal-collection-list');
    collectionDiv.innerHTML = "";
    
    Object.keys(globalItemsData).forEach(itemKey => {
        const itemName = globalItemsData[itemKey].name;
        // Check if user has this item, default to 0
        const count = (user.collectedItems && user.collectedItems[itemKey]) ? user.collectedItems[itemKey] : 0;

        collectionDiv.innerHTML += `
            <div class="collection-row">
                <label>${itemName}:</label>
                <input type="number" class="collection-input" data-item="${itemKey}" value="${count}">
            </div>
        `;
    });

    // 3. Build Vouchers List
    const vouchersDiv = document.getElementById('modal-vouchers-list');
    vouchersDiv.innerHTML = "";
    if (user.myVouchers) {
        Object.keys(user.myVouchers).forEach(voucherId => {
            const v = user.myVouchers[voucherId];
            vouchersDiv.innerHTML += `
                <div class="voucher-row">
                    <div>
                        <strong>${v.description}</strong><br>
                        <small>Code: ${v.code}</small>
                    </div>
                    <button class="delete-btn" onclick="deleteVoucher('${userId}', '${voucherId}')">Delete</button>
                </div>
            `;
        });
    } else {
        vouchersDiv.innerHTML = "<small>No active vouchers.</small>";
    }
};

window.closeUserModal = () => {
    document.getElementById('user-modal').style.display = "none";
};

// SAVE User Changes (Scans & Collections)
window.saveUserChanges = () => {
    const userId = document.getElementById('modal-userid').value;
    const newScans = parseInt(document.getElementById('modal-scans').value);
    
    // Create updates object
    const updates = {};
    updates[`users/${userId}/totalScans`] = newScans;

    // Loop through collection inputs to save counts
    const inputs = document.querySelectorAll('.collection-input');
    inputs.forEach(input => {
        const itemKey = input.getAttribute('data-item');
        const val = parseInt(input.value);
        updates[`users/${userId}/collectedItems/${itemKey}`] = val;
    });

    update(ref(db), updates)
        .then(() => {
            alert("User updated successfully!");
            window.closeUserModal();
        })
        .catch(err => alert("Error: " + err.message));
};

// DELETE Voucher
window.deleteVoucher = (userId, voucherId) => {
    if(confirm("Are you sure you want to delete this voucher? This cannot be undone.")) {
        remove(ref(db, `users/${userId}/myVouchers/${voucherId}`))
            .then(() => {
                // Refresh the modal to show it's gone
                window.openUserModal(userId); 
            })
            .catch(err => alert(err.message));
    }
};

// --- ITEM CONFIG LOGIC ---

window.updateItem = (itemKey) => {
    const newRate = document.getElementById(`rate-${itemKey}`).value;
    const newThresh = document.getElementById(`thresh-${itemKey}`).value;
    
    const updates = {};
    updates[`game_data/items/${itemKey}/dropRate`] = parseInt(newRate);
    updates[`game_data/items/${itemKey}/rewardThreshold`] = parseInt(newThresh);

    update(ref(db), updates)
        .then(() => alert('Item configuration saved!'))
        .catch((error) => alert(error.message));
};

// --- NAVIGATION ---
window.showSection = (sectionId) => {
    document.querySelectorAll('.section').forEach(el => el.style.display = 'none');
    document.querySelectorAll('.sidebar li').forEach(el => el.classList.remove('active'));
    document.getElementById(sectionId + '-section').style.display = 'block';
};