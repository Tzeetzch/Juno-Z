const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch();
    const page = await browser.newPage();

    console.log('=== Testing Juno Bank ===\n');

    // Test 1: Login page
    console.log('1. Opening login page...');
    await page.goto('http://localhost:5000/login');
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'screenshots/01-login.png' });
    console.log('   Screenshot: screenshots/01-login.png');

    // Test 2: Parent login page
    console.log('2. Going to parent login...');
    await page.click('text=Parent');
    await page.waitForTimeout(1000);
    await page.screenshot({ path: 'screenshots/02-parent-login.png' });
    console.log('   Screenshot: screenshots/02-parent-login.png');

    // Test 3: Try parent login
    console.log('3. Logging in as Dad...');
    await page.fill('input[type="email"]', 'dad@junobank.local');
    await page.fill('input[type="password"]', 'parent123');
    await page.screenshot({ path: 'screenshots/03-parent-filled.png' });
    await page.click('text=Login');
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'screenshots/04-parent-loggedin.png' });
    console.log('   Screenshot: screenshots/04-parent-loggedin.png');

    // Check URL after login
    const url = page.url();
    console.log('   Current URL:', url);
    if (url === 'http://localhost:5000/' || url.includes('localhost:5000')) {
        console.log('   ✅ Parent login SUCCESS\n');
    } else {
        console.log('   ❌ Parent login FAILED\n');
    }

    // Logout
    console.log('4. Logging out...');
    const logoutBtn = await page.$('button:has([data-icon="logout"])');
    if (logoutBtn) {
        await logoutBtn.click();
        await page.waitForTimeout(1000);
    }

    // Test 5: Child login
    console.log('5. Testing child login...');
    await page.goto('http://localhost:5000/login/child');
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'screenshots/05-child-login.png' });
    console.log('   Screenshot: screenshots/05-child-login.png');

    // Get all picture buttons
    const buttons = await page.$$('.picture-btn');
    console.log('   Found', buttons.length, 'picture buttons');

    await browser.close();
    console.log('\n=== Tests Complete ===');
    console.log('Check the screenshots/ folder to see the results.');
})();
