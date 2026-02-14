
import { Step, BeforeScenario, AfterScenario } from "gauge-ts";
import axios from "axios";
import {
    openBrowser,
    closeBrowser,
    goto,
    waitFor,
    text,
    textBox,
    write,
    press,
} from "taiko";
import { URL } from "url";

type TestContext = {
    apiUrl: string;
    frontendUrl: string;
    loginPath: string;
    loginUrl: string;
    providerEmail?: string;
    providerPassword?: string;
};

const context: TestContext = {
    apiUrl: process.env.API_URL ?? "http://localhost:7071/api",
    frontendUrl: process.env.FRONTEND_URL ?? "http://localhost:5173",
    loginPath: process.env.FRONTEND_LOGIN_PATH ?? "/login",
    loginUrl: "",
};

const HEADLESS = (process.env.headless_chrome ?? "true").toLowerCase() === "true";
const providerPassword = process.env.DEFAULT_PROVIDER_PASSWORD ?? "GaugePass123!";

export default class StepImplementation {
    @BeforeScenario()
    public async beforeScenario() {
        await openBrowser({
            headless: HEADLESS,
            args: ["--no-sandbox", "--disable-gpu"],
        });
        context.loginUrl = new URL(context.loginPath, context.frontendUrl).toString();
        context.providerEmail = undefined;
        context.providerPassword = undefined;
    }

    @AfterScenario()
    public async afterScenario() {
        await closeBrowser();
    }

    @Step("Given the backend API is reachable")
    public async backendReachable() {
        try {
            await axios.get(context.apiUrl, { timeout: 5000 });
        } catch (error) {
            if (axios.isAxiosError(error) && error.code === "ECONNREFUSED") {
                throw new Error(`Unable to reach backend API at ${context.apiUrl}. Is the functions host running?`);
            }
            if (axios.isAxiosError(error) && error.response) {
                return;
            }
            throw error;
        }
    }

    @Step("And the frontend login page is accessible")
    public async loginPageAccessible() {
        await goto(context.loginUrl);
        await waitFor(async () => {
            if (!(await text("Kopitra").exists())) {
                throw new Error("The login page branding did not render.");
            }
            return true;
        }, 30000);
    }

    @Step("When I create a temporary provider account with password GaugePass123!")
    public async createProvider() {
        const timestamp = Date.now();
        const email = `gauge-provider-${timestamp}@example.com`;
        const name = `Gauge Provider ${timestamp}`;

        context.providerEmail = email;
        context.providerPassword = providerPassword;

        const client = axios.create({
            baseURL: context.apiUrl,
            timeout: 10000,
            validateStatus: () => true,
        });

        const response = await client.post("/auth/register", {
            email,
            password: providerPassword,
            name,
        });

        if (response.status >= 500) {
            throw new Error(`Provider registration failed with status ${response.status}`);
        }
    }

    @Step("And I log in through the UI using that account")
    public async loginViaUi() {
        if (!context.providerEmail || !context.providerPassword) {
            throw new Error("Provider credentials were never set.");
        }

        await goto(context.loginUrl);

        let emailField = textBox("メールアドレス");
        if (!(await emailField.exists(0, 0))) {
            emailField = textBox({ placeholder: "Email" });
            if (!(await emailField.exists(0, 0))) {
                throw new Error("Email input field is not available on the login screen.");
            }
        }

        let passwordField = textBox("パスワード");
        if (!(await passwordField.exists(0, 0))) {
            passwordField = textBox({ placeholder: "Password" });
            if (!(await passwordField.exists(0, 0))) {
                throw new Error("Password input field is not available on the login screen.");
            }
        }

        await write(context.providerEmail, emailField);
        await write(context.providerPassword, passwordField);
        await press("Enter");

        await waitFor(async () => {
            if (!(await text("ダッシュボード").exists())) {
                throw new Error("Dashboard navigation did not appear after login.");
            }
            return true;
        }, 30000);
    }

    @Step("Then I should see the dashboard navigation")
    public async seeDashboardNav() {
        await waitFor(async () => {
            if (!(await text("ダッシュボード").exists())) {
                throw new Error("Dashboard navigation is not visible.");
            }
            return true;
        }, 30000);
    }
}
