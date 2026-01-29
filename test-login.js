const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch();
    const page = await browser.newPage();

    console.log('Testing parent login flow...\n');

    // Go to parent login
    await page.goto('http://localhost:5000/login/parent');
    await page.waitForTimeout(1000);

    // Fill and submit
    await page.fill('input[type="email"]', 'dad@junobank.local');
    await page.fill('input[type="password"]', 'parent123');

    console.log('Before clicking login:');
    console.log('  URL:', page.url());

    // Click login and wait for navigation
    await page.click('button:has-text("Login")');
    await page.waitForTimeout(3000);

    console.log('\nAfter clicking login:');
    console.log('  URL:', page.url());

    // Take screenshot
    await page.screenshot({ path: 'screenshots/login-result.png' });

    // Check page content
    const content = await page.textContent('body');
    if (content.includes('Hi,')) {
        console.log('  ✅ Login worked - user greeting visible');
    } else if (content.includes('Invalid')) {
        console.log('  ❌ Login failed - invalid credentials message');
    } else {
        console.log('  ⚠️  Unknown state');
        console.log('  Page text preview:', content.substring(0, 200));
    }

    await browser.close();
})();
