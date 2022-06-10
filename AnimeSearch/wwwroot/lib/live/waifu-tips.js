/*
 * Live2D Widget
 * https://github.com/stevenjoezhang/live2d-widget
 */

class LiveWidget
{
    /** @type LiveWidget */
    static #LiveModel = null;

    static getLiveModel(config = { waifuPath: "", cdnPath: "" }, apiPath = "")
    {
        if (LiveWidget.#LiveModel == null)
        {
            LiveWidget.#LiveModel = new LiveWidget();
            LiveWidget.#LiveModel.#init(config, apiPath);
        }

        return LiveWidget.#LiveModel;
    }

    #modelList = null;
    #messageTimer = 0;
    #CDNPath = "";
    #toggle = null;
    #config = null;
    #isOn = true;
    
    constructor()
    {
        
    }

    /**
     * 
     * @param {string | {waifuPath: "", apiPath: ""}} config
     * @param {string} apiPath
     */
    #init(config, apiPath)
    {
        if (typeof config === "string") {
            config = {
                waifuPath: config,
                apiPath
            };
        }

        this.#config = config;

        document.body.insertAdjacentHTML("beforeend", `<div id="waifu-toggle">
			<span>Revient !</span>
		</div>`);

        this.#toggle = document.getElementById("waifu-toggle");

        this.#toggle.addEventListener("click", this.toggle.bind(this));

        if (localStorage.getItem("waifu-display") && Date.now() - localStorage.getItem("waifu-display") <= 86400000) {
            this.#toggle.setAttribute("first-time", true);

            this.#isOn = false;

            setTimeout(() => {
                this.#toggle.classList.add("waifu-toggle-active");
            }, 0);
        }
        else {
            this.#isOn = true;

            this.#loadWidget(config);
        }
    }

    toggle()
    {
        this.#isOn = !this.#isOn;

        if (this.#isOn)
        {
            this.#toggle.classList.remove("waifu-toggle-active");

            if (this.#toggle.getAttribute("first-time"))
            {
                this.#loadWidget(this.#config);
                this.#toggle.removeAttribute("first-time");
            }
            else
            {
                localStorage.removeItem("waifu-display");
                document.getElementById("waifu").style.display = "";

                setTimeout(() => {
                    document.getElementById("waifu").style.bottom = "55px";
                }, 0);
            }
        }
        else
        {
            localStorage.setItem("waifu-display", Date.now());

            this.showMessage("Au revoir !", 2000, 11);

            document.getElementById("waifu").style.bottom = "-1000px";

            setTimeout(() => {
                document.getElementById("waifu").style.display = "none";
                document.getElementById("waifu-toggle").classList.add("waifu-toggle-active");
            }, 3000);
        }
    }


    /**
     *
     * @param {{waifuPath: "", apiPath: "", cdnPath: ""}} config
     */
    #loadWidget(config)
    {
        let { waifuPath, apiPath, cdnPath } = config;

        const conf = document.getElementById("liveConf");

        if (typeof cdnPath === "string") {
            if (!cdnPath.endsWith("/")) cdnPath += "/";
        }
        else if (typeof apiPath === "string") {
            if (!apiPath.endsWith("/")) apiPath += "/";
        }
        else {
            console.error("Invalid initWidget argument!");
            return;
        }

        this.#CDNPath = cdnPath;

        localStorage.removeItem("waifu-display");
        sessionStorage.removeItem("waifu-text");

        document.body.insertAdjacentHTML("beforeend", `<div id="waifu">
			<div id="waifu-tips"></div>
			<canvas id="live2d" width="800" height="800"></canvas>
            <div id="waifu-tool" style="right: -10px;">
				<span class="fa fa-lg fa-street-view"></span>
				<span class="fa fa-lg fa-info-circle"></span>
				<span class="fa fa-lg fa-times"></span>
			</div>
		</div>`);

        setTimeout(() => {
            var waifu = document.getElementById("waifu");
            var canvas = document.getElementById("live2d");
            var tools = document.getElementById("waifu-tool");

            waifu.style.bottom = "61px";
            waifu.style.left = 0;

            canvas.style.width = "300px";
            canvas.style.height = "300px";

            if (conf != undefined) {
                waifu.style.left = conf.style.left;
                waifu.style.right = conf.style.right;

                if (conf.style.right != "") {
                    tools.style.left = "-10px";
                    tools.style.right = "";
                }

                if (conf.style.bottom != "")
                    waifu.style.bottom = conf.style.bottom;

                canvas.style.height = conf.style.height;
                canvas.style.width = conf.style.width;
            }

        }, 0);

        let userAction = false,
            userActionTimer = 0,
            messageArray = ["Cela fait longtemps, les jours sont passés si vite...", "J'ai failli m'ennuyer", "Tu es parti ? &#x1F622;", "Surtout ne revient jamais !", "N'oubliez pas de mettre Adblock sur liste blanche !"];

        window.addEventListener("mousemove", () => userAction = true);
        window.addEventListener("keydown", () => userAction = true);

        setInterval(() => {
            if (userAction) {
                userAction = false;
                clearInterval(userActionTimer);
                userActionTimer = null;
            }
            else if (!userActionTimer) {
                userActionTimer = setInterval(() => {
                    this.showMessage(randomSelection(messageArray), 6000, 9);
                }, 20000);
            }
        }, 1000);

        this.#registerEventListener();

        this.welcomeMessage();

        let modelId = localStorage.getItem("modelId"),
            modelTexturesId = localStorage.getItem("modelTexturesId");

        if (modelId === null) {
            modelId = 5; // model ID
            modelTexturesId = 2; // texture ID (seconde dimension)
        }

        this.loadModel(modelId, modelTexturesId);

        fetch(waifuPath).then(response => response.json()).then(result =>
        {
            window.addEventListener("mouseover", event =>
            {
                for (let { selector, text } of result.mouseover)
                {
                    if (!event.target.matches(selector)) continue;

                    text = randomSelection(text);
                    text = text.replace("{text}", event.target.innerText);
                    text = text.replace("{title}", event.target.title);

                    this.showMessage(text, 4000, 8);

                    return;
                }
            });

            window.addEventListener("click", event =>
            {
                for (let { selector, text } of result.click)
                {
                    if (!event.target.matches(selector)) continue;

                    text = randomSelection(text);
                    text = text.replace("{text}", event.target.innerText);

                    this.showMessage(text, 4000, 8);

                    return;
                }
            });

            result.seasons.forEach(({ date, text }) =>
            {
                const now = new Date(),
                    after = date.split("-")[0],
                    before = date.split("-")[1] || after;

                if ((after.split("/")[0] <= now.getMonth() + 1 && now.getMonth() + 1 <= before.split("/")[0]) && (after.split("/")[1] <= now.getDate() && now.getDate() <= before.split("/")[1]))
                {
                    text = randomSelection(text);
                    text = text.replace("{year}", now.getFullYear());

                    var index = text.indexOf("|age-")

                    if (index >= 0)
                    {
                        var indexEnd = text.indexOf("|", index + 1);

                        if (indexEnd >= 0)
                        {
                            var date = text.substring(index + 5, indexEnd);
                            text = text.substring(0, index) + (now.getFullYear() - date) + text.substring(indexEnd + 1);
                        }
                    }

                    this.showMessage(text, 7000, 100);
                    messageArray.push(text);
                }
            });
        });
    }

    welcomeMessage()
    {
        let text;

        if (location.pathname === "/") {
            const now = new Date().getHours();

            if (now > 5 && now <= 7) text = "Une bonne journée est sur le point de commencer. (Même si il est encore un peu tôt)";
            else if (now > 7 && now <= 11) text = "Good morning ! Le travail se passe bien, ne restez pas assis, levez-vous et marchez !";
            else if (now > 11 && now <= 13) text = "Bon Appétit !";
            else if (now > 13 && now <= 17) text = null;
            else if (now > 17 && now <= 19) text = "C'est le soir ! La vue du coucher de soleil à l'extérieur est magnifique, la plus belle chose est le coucher de soleil rouge.";
            else if (now > 19 && now <= 21) text = null;
            else if (now > 21 && now <= 23) text = "Tu vas commencer à regarder quelque chose à cette heure-là .";
            else text = "Est-tu un vampire &#x1F628; ? <br/> Tu es debout si tard, tu peux te lever demain matin ?";
        }
        else {
            text = `Bienvenue sur la page <span>「${document.title.split(" - ")[0]}」</span>`;
        }

        if (text != null)
            this.showMessage(text, 7000, 8);
    }

    async loadModelList()
    {
        const response = await fetch(`${this.#CDNPath}model_list.json`);
        this.#modelList = await response.json();

        //this.modelList.models.forEach(m => console.log(m))
    }

    async loadModel(modelId, modelTexturesId, message)
    {
        localStorage.setItem("modelId", modelId);
        localStorage.setItem("modelTexturesId", modelTexturesId);

        this.showMessage(message, 4000, 10);

        if (!this.#modelList) await this.loadModelList();

        const target = randomSelection(this.#modelList.models[modelId]);
        //console.log("target: " + target);
        loadlive2d("live2d", `${this.#CDNPath}model/${target}/index.json`);
    }

    async loadRandModel()
    {
        const modelId = localStorage.getItem("modelId"),
            modelTexturesId = localStorage.getItem("modelTexturesId");

        if (!this.#modelList) await this.loadModelList();

        const target = randomSelection(this.#modelList.models[modelId]);

        loadlive2d("live2d", `${this.#CDNPath}model/${target}/index.json`);
        this.showMessage("random model？", 4000, 10);
    }

    async loadOtherModel()
    {
        let modelId = localStorage.getItem("modelId");

        if (!this.#modelList) await this.loadModelList();

        const index = (++modelId >= this.#modelList.models.length) ? 0 : modelId;
        this.loadModel(index, 0, this.#modelList.messages[index]);
    }

    #registerEventListener()
    {
        window.addEventListener("copy", () => {
            this.showMessage("Qu'avez-vous copié ? N'oubliez pas d'ajouter la source !", 6000, 9);
        });

        window.addEventListener("visibilitychange", () => {
            if (!document.hidden) this.showMessage("Wow, tu es enfin de retour.", 6000, 9);
        });

        document.querySelector("#waifu-tool .fa-street-view").addEventListener("click", this.loadOtherModel.bind(this));

        document.querySelector("#waifu-tool .fa-times").addEventListener("click", this.toggle.bind(this));

        document.querySelector("#waifu-tool .fa-info-circle").addEventListener("click", () => {
            SwalFire({
                icon: "info",
                title: "Live2D infos",
                html: '<p>Lien git des créteurs: <a target="_blank" href="https://github.com/stevenjoezhang/live2d-widget">https://github.com/stevenjoezhang/live2d-widget</a></p>' +
                    '<p>Lien du git de certains model 2D: <a target="_blank" href="https://github.com/DomathID/live2d-model">https://github.com/DomathID/live2d-model</a></p>'
            });
        });
    }

    /**
     * 
     * @param {string} text
     * @param {number} timeout
     * @param {number} priority
     */
    showMessage(text, timeout, priority)
    {
        if (!text || (sessionStorage.getItem("waifu-text") && sessionStorage.getItem("waifu-text") > priority))
            return;

        if (this.#messageTimer) {
            clearTimeout(this.#messageTimer);
            this.#messageTimer = null;
        }

        text = randomSelection(text);
        sessionStorage.setItem("waifu-text", priority);

        const tips = document.getElementById("waifu-tips");

        tips.innerHTML = text;
        tips.classList.add("waifu-tips-active");

        this.#messageTimer = setTimeout(() => {
            sessionStorage.removeItem("waifu-text");
            tips.classList.remove("waifu-tips-active");
        }, timeout);
    }
}

function randomSelection(obj) {
    return Array.isArray(obj) ? obj[Math.floor(Math.random() * obj.length)] : obj;
}