const ThemeStorageKey = 'blazixbaseui-demo-theme';
const ThemeCookieName = 'blazixbaseui_demo_theme';
const ThemeWindowNamePrefix = 'blazixbaseui-demo-theme:';

let demoShellInitialized = false;

function normalizeTheme(theme) {
    return theme === 'dark' ? 'dark' : 'light';
}

function getShells() {
    return document.querySelectorAll('.demo-app-shell');
}

function setDocumentTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.body?.setAttribute('data-theme', theme);
}

function getCookieTheme() {
    try {
        const themeCookie = document.cookie
            .split('; ')
            .find((cookie) => cookie.startsWith(`${ThemeCookieName}=`));

        if (!themeCookie) {
            return null;
        }

        return normalizeTheme(decodeURIComponent(themeCookie.split('=').slice(1).join('=')));
    } catch {
        return null;
    }
}

function getWindowNameTheme() {
    try {
        if (!window.name.startsWith(ThemeWindowNamePrefix)) {
            return null;
        }

        return normalizeTheme(window.name.slice(ThemeWindowNamePrefix.length));
    } catch {
        return null;
    }
}

function setCookieTheme(theme) {
    try {
        document.cookie = `${ThemeCookieName}=${encodeURIComponent(theme)}; Max-Age=31536000; Path=/; SameSite=Lax`;
    } catch {
        // Some embedded browser surfaces expose cookies as read-only.
    }
}

function setWindowNameTheme(theme) {
    try {
        window.name = `${ThemeWindowNamePrefix}${theme}`;
    } catch {
        // Ignore storage failures in constrained browser contexts.
    }
}

export function getDemoTheme() {
    try {
        const storedTheme = window.localStorage?.getItem(ThemeStorageKey);

        if (storedTheme) {
            return normalizeTheme(storedTheme);
        }
    } catch {
        // Some browsers block localStorage in private or restricted contexts.
    }

    return getCookieTheme() ?? getWindowNameTheme() ?? 'light';
}

export function setDemoTheme(theme) {
    const nextTheme = normalizeTheme(theme);

    try {
        window.localStorage?.setItem(ThemeStorageKey, nextTheme);
    } catch {
        // Some browsers block localStorage in private or restricted contexts.
    }

    setCookieTheme(nextTheme);
    setWindowNameTheme(nextTheme);
    setDocumentTheme(nextTheme);

    getShells().forEach((element) => {
        element.setAttribute('data-theme', nextTheme);
    });
}

export function setMobileNavOpen(open) {
    const nextValue = open ? 'true' : 'false';

    getShells().forEach((element) => {
        element.setAttribute('data-nav-open', nextValue);
    });
}

export function initializeDemoShell() {
    setDemoTheme(getDemoTheme());

    if (demoShellInitialized) {
        return;
    }

    demoShellInitialized = true;

    document.addEventListener('click', (event) => {
        const target = event.target instanceof Element ? event.target : event.target?.parentElement;

        if (target?.closest('[data-demo-mobile-scrim]') || target?.closest('.demo-nav__link')) {
            setMobileNavOpen(false);
        }
    });

    window.addEventListener('resize', () => {
        if (window.matchMedia('(min-width: 1024px)').matches) {
            setMobileNavOpen(false);
        }
    });

    window.Blazor?.addEventListener?.('enhancedload', () => {
        setDemoTheme(getDemoTheme());
        setMobileNavOpen(false);
    });
}
