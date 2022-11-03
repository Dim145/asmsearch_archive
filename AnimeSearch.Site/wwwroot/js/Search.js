(() =>
{
    let aId = 0;
    let sId = 0;
    
    $(document).ready(function()
    {
        $('#ba').on('hidden.bs.modal', function ()
        {
            var frame = $("#frame");
            frame.data("link", frame.attr("src"));
            frame.attr("src", "");

        });

        $("#btnBa").click(function ()
        {
            var frame = $("#frame");

            if (frame.data("link") != undefined && frame.data("link") != "")
            {
                frame.attr("src", frame.data("link"));
                frame.data("link", "");
            }
        });

        $('#modal')
            .on('hide', function ()
            {
                console.log('hide');
            })
            .on('hidden', function ()
            {
                console.log('hidden');
            })
            .on('show', function ()
            {
                console.log('show');
            })
            .on('shown', function ()
            {
                console.log('shown')
            });

        $("#ere").click(function()
        {
            const iconLoading = $("#rsIcon");
            const send = $("#send");

            iconLoading.show();
            send.attr('disable', true);

            this.disabled = true;

            scrapper.parseTable();
            scrapper.visitUnvisited((_) => reFillSelects()).then(() =>
            {
                reFillSelects();
                iconLoading.hide();
                this.disabled = false;
                send.attr('disable', false);
            });
        });

        $("#btnCloseEps").click(() =>
        {
            $("#ep").attr("src", "");
            const select = $("#episodes");

            select.val("");
            select.change();
        });

        $("#openEps").click(() =>
        {
            if(Object.keys(seasons).length <= 0)
            {
                $("#ere").click();
            }
        });
        
        const value = $("input[type='hidden']").val().split(".");
        aId = value[0];
        sId = value[1];
        
        $("#send").click(() =>
        {
            const seasons = scrapper.seasons;
            
            $.post("/save_episodes", {aId: aId, sId: sId, datas: seasons});
        });
        
        if(baseEps && Object.keys(baseEps).length > 0)
        {
            for (const tmpKey in baseEps)
                seasons[tmpKey] = baseEps[tmpKey];
            
            reFillSelects();
        }
    });

    class EpScrapper
    {
        /**
         * @type {[{url: string, js: string, nbResult: number}]}
         */
        #sites = []

        /**
         * @type {[{url: string, js: string, nbResult: number}]}
         */
        #visitedSites = []

        #savedRes = {}

        static async #load(url)
        {
            const QueryURL = "https://api.codetabs.com/v1/proxy/?quest=" + url;
            try
            {
                let data = await fetch(QueryURL);

                if (data)
                {
                    const res = await data.text()

                    if (res.length > 0)
                    {
                        return res;
                    }
                }
            }
            catch (e)
            {
                console.error(e);
            }

            return undefined;
        }

        parseTable()
        {
            /**
             * @type {[{url: string, js: string, nbResult: number}]}
             */
            const sites = [];

            $("tr").each(function ()
            {
                const tr = $(this);

                const nbResult = ((tr.children()[3] || {}).firstChild || {}).innerText;

                if(nbResult == "1")
                {
                    const js  = ($(((tr.children()[3] || {}).firstChild || {})).attr('onclick') || "").toString();
                    const url = js.substring(js.indexOf('"')+1, js.lastIndexOf('"'));

                    sites.push({nbResult: parseInt(nbResult), js: js, url: url});
                }
            });

            this.#sites = sites;
        }

        /**
         *
         * @param {Function<{}>} callback
         * @returns {Promise<{}>}
         */
        async visitUnvisited(callback)
        {
            const toVisit = this.#sites.filter(s => !this.#visitedSites.find(si => si.url === s.url));

            const resDatas = {};

            for (const site of toVisit)
            {
                this.#visitedSites.push(site);

                try
                {
                    resDatas[site.url] = await this.#searchInPage(site, true);

                    if(this.#savedRes[site.url])
                        this.#savedRes[site.url].push(...resDatas[site.url]);
                    else
                        this.#savedRes[site.url] = resDatas[site.url];

                    if(callback)
                        callback.call(resDatas[site.url]);
                }
                catch (e)
                {
                    console.error(e);
                }
            }

            return resDatas;
        }

        /**
         * @param {{url: string, js: string, nbResult: number}} site
         * @param {boolean} forceSearch
         */
        async #searchInPage(site, forceSearch = false)
        {
            const result = await EpScrapper.#load(site.url);

            if(!result)
                return [];

            /**
             * @type {{url: string, ep: string, saison: string}[]}
             */
            const iframes = [];

            iframes.push(...this.#findUrls(result, "iframe"));

            const parsed = this.parseUrl(site.url);

            iframes.forEach(r =>
            {
                if(r.ep !== "--1")
                    return;

                ["episode", "ep"].forEach(e =>
                {
                    if(site.url.includes(e))
                    {
                        let index = site.url.indexOf(e) + e.length;
                        let match = site.url.substring(index).match(e + "[-_]?[0-9]+");

                        if(match && match.length > 0)
                            r.ep = match[0].substring(e.length+(isNaN(parseInt(match[0][0])) ? 1 : 0));
                    }
                });

                if(r.ep === "--1" && parsed)
                {
                    r.ep = parsed.ep;
                    r.saison = parsed.s;
                }
            });

            if(iframes.length === 0 && forceSearch)
            {
                const as = await this.#descendSearch(result, site.url);

                iframes.push(...as);
            }

            return iframes;
        }

        /**
         * @param {string} html
         * @param {string} originUrl
         * @returns {Promise<{url: string, ep: string, saison: string}[]>}
         */
        async #descendSearch(html, originUrl)
        {
            /**
             * @type {{url: string, ep: string, saison: string}[]}
             */
            const iframes = [];

            /**
             * @type {string[]}
             */
            let urls = [];

            try
            {
                if(JSON.parse(html)["Error"])
                    return iframes;
            }
            catch (e)
            {

            }

            let originUri = undefined;

            try
            {
                originUri = new URL(originUrl);
            }
            catch (e)
            {
                return iframes;
            }

            const tmp = originUrl.split('/').filter(s => s.length > 0);
            const parsedOrigin = this.parseUrl(originUrl) || {name: tmp[tmp.length-1]};

            let index = html.indexOf("<a");

            while(index > -1)
            {
                let indexEnd = html.indexOf(">", index);

                if(indexEnd > index)
                {
                    const a = $(html.substring(index, indexEnd+1));
                    let url = (a.attr('href') || "").replaceAll(/(&amp;|&#038;)/g, "&");

                    if(!url.startsWith("http"))
                        url = (originUri.origin.endsWith("/") || url.startsWith("/") ? originUri.origin : originUri.origin + "/") + url;

                    const parsed = this.parseUrl(url);

                    // todo improve
                    if(((parsed ? parsedOrigin.name.includes(parsed.name) : false) || url.match("episode[\-]?[0-9]+") || url.match("ep[\-]?[0-9]+")) && !urls.find(u => u == url) && !this.allEps.find(r => r.url == url))
                        urls.push(url)
                }

                index = html.indexOf("<a", indexEnd);
            }

            urls = [...new Set(urls)]

            for (const url of urls)
            {
                const page = await EpScrapper.#load(url);
                const data = this.#findUrls(page, "iframe");

                const parsed = this.parseUrl(url);

                if(data.length > 0)
                {
                    data.forEach(r =>
                    {
                        if(r.ep !== "--1")
                            return;

                        ["episode", "ep"].forEach(e =>
                        {
                            if(url.includes(e))
                            {
                                let index = url.indexOf(e);
                                let match = url.substring(index).match(e + "[-_]?[0-9]+");

                                if(match && match.length > 0)
                                    r.ep = match[0].substring(e.length+(isNaN(parseInt(match[0][0])) ? 1 : 0));
                            }
                        });

                        r.source = "a(" + url + ")";

                        if(r.ep === "--1" && parsed)
                            r.ep = parsed.ep;

                        if(r.saison === "1" && parsed)
                            r.saison = parsed.s;
                    });

                    iframes.push(...data);
                }
            }

            return iframes;
        }

        /**
         * @param {string} element
         * @param {string} selector
         * @returns {{url: string, ep: string, saison: string}[]}
         */
        #findUrls(element, selector)
        {
            /**
             * @type {{url: string, ep: string, saison: string}[]}
             */
            const elements = [];

            /**
             * @type {string[]}
             */
            const iframes = [];
            let index = element.indexOf("<" + selector);

            while(index > -1)
            {
                const indexEnd = element.indexOf(">", index);

                if(indexEnd > -1 && indexEnd > index)
                {
                    iframes.push(element.substring(index, indexEnd+1));
                }

                index = element.indexOf("<" + selector, indexEnd);
            }

            for (const str of iframes)
            {
                const start = str.indexOf("http") || -1;
                const end = str.indexOf('"', start) || -1;

                let url = "";

                if(start >= 0 && end > start)
                {
                    url = (str.substring(start, end) || "").replaceAll(/(&amp;|&#038;)/g, "&");

                    if(url.length > 0 && url.startsWith("http"))
                    {
                        try
                        {
                            const uri = new URL(decodeURIComponent(url).replaceAll("&amp;", "&"));

                            if(uri.protocol.startsWith("http") && uri.origin)
                                url = uri.href;
                        }
                        catch (e)
                        {
                            console.error(e);
                        }
                    }
                }
                else
                {
                    const iframe = $(str);

                    url = iframe.attr('src') || iframe.attr('href');

                    if(url)
                    {
                        url = url.startsWith("//") ? url.substring(2) : url;
                    }
                    else
                    {
                        continue;
                    }
                }

                if(!url.startsWith("http"))
                    url = "https://" + url;

                let episode = "--1";
                let saison  = "1";

                ["episode", "ep"].forEach(e =>
                {
                    if(url.includes(e))
                    {
                        let index = url.indexOf(e) + e.length;
                        let match = url.substring(index).match(e + "[-_]?[0-9]+");

                        if(match && match.length > 0)
                            episode = match[0].substring(e.length+1);
                    }
                });

                ["saison", "season"].forEach(e =>
                {
                    if(url.includes(e))
                    {
                        let index = url.indexOf(e) + e.length;
                        let match = url.substring(index).match(e + "[-_]?[0-9]+");

                        if(match && match.length > 0)
                            saison = match[0].substring(e.length+1);
                    }
                });

                if(episode === "--1")
                {
                    const seasonMath = url.match("saison[-_]?[0-9]+");

                    if(seasonMath && seasonMath.length > 0)
                    {
                        saison = seasonMath[0].substring(isNaN(seasonMath[0][6]) ? 7 : 6)

                        const epMatch = url.substring(seasonMath["index"] + seasonMath[0].length).match("[-_]?[0-9]+");

                        if(epMatch && epMatch.length > 0)
                            episode = epMatch[0].substring(isNaN(epMatch[0][0]) ? 1 : 0);
                    }
                }

                if(!elements.find(e => e.url == url))
                    elements.push({url: url, ep: episode, saison: saison, source: selector});
            }

            return elements;
        }

        /**
         *
         * @param {string} url
         */
        parseUrl(url)
        {
            const path = url.split('/').filter(s => !s.length == 0);
            let probableName = path[path.length-1];

            if(!probableName.match("[a-zA-Z]+") && !isNaN(parseInt(probableName)))
                return {ep: parseInt(probableName), s: 1, name: path[path.length-2]};

            let match = probableName.match("[a-zA-Z\-]+-[0-9]+");

            if(match)
            {
                const s = match[0];
                const indexT = s.lastIndexOf('-');

                const res = {ep: parseInt(s.substring(indexT + 1)), name: s.substring(0, indexT), s: 1};

                if(res.name.endsWith("saison") || res.name.endsWith("s"))
                {
                    res.s = res.ep;
                    res.name = res.name.substring(0, res.name.lastIndexOf('-'));

                    const epMatch = probableName.substring(match["index"] + s.length).match("-[0-9]+");

                    if(epMatch)
                        res.ep = parseInt(epMatch[0].substring(1))
                }

                return res;
            }

            return undefined;
        }

        AddEpInFrame()
        {
            this.setEpInFrame(1, 0)
        }

        /**
         * @param {number} episodeNumber
         * @param {number} indexUrl
         */
        setEpInFrame(episodeNumber, indexUrl)
        {
            const datas = this.allEps;

            if(datas.length > 0)
            {
                const frame = $("#ep");
                const ep = datas.filter(r => r.ep == episodeNumber);

                indexUrl = Math.max(indexUrl, 0);
                indexUrl = Math.min(indexUrl, ep.length - 1);

                if(ep.length > 0 && frame.attr("src") !== ep[indexUrl].url)
                    frame.attr("src", ep[indexUrl].url);
            }
        }

        /**
         * @returns {{url: string, ep: string}[]}
         */
        get resData() {return this.#savedRes;}

        /**
         * @returns {{url: string, ep: string, saison: string}[]}
         */
        get allEps() {return Object.values(this.#savedRes).flatMap(v => v);}

        get seasons()
        {
            const eps = this.allEps;

            const maxSeason = Math.max(...eps.map(r => parseInt(r.saison)).filter(n => !isNaN(n)));

            const seasons = {};

            for (let i = 1; i <= maxSeason; i++)
            {
                const maxEp = Math.max(...eps.filter(r => r.saison == i).map(r => parseInt(r.ep)).filter(n => !isNaN(n)));

                const episodes = {};

                for (let j = 1; j <= maxEp; j++)
                    episodes[j] = eps.filter(r => r.ep == j).map(r => r.url);

                episodes["others"] = eps.filter(r => r.ep === "--1" && r.saison == i).map(r => r.url);

                seasons[i] = episodes;
            }

            return seasons;
        }
    }

    const scrapper = new EpScrapper();


    function performCallback()
    {
        scrapper.parseTable();
        scrapper.visitUnvisited((_) => reFillSelects()).then(r =>
        {
            console.log(r);

            reFillSelects();
        });
    }

    const seasons = {};

    function reFillSelects()
    {
        const tmp = scrapper.seasons;

        for (const tmpKey in tmp)
            seasons[tmpKey] = tmp[tmpKey];

        const selectSeason = $("#seasons");
        const selectEps    = $("#episodes");
        const selectUrls   = $("#urls");

        selectSeason.empty()

        for (const seasonsKey in seasons)
        {
            const option = $("<option>");

            option.attr("value", seasonsKey);
            option.append("saison " + seasonsKey);

            selectSeason.append(option);
        }

        selectSeason.change((e) =>
        {
            selectEps.empty();
            selectEps.append($("<option>"));

            for (const episodesKey in seasons[e.target.value])
            {
                const option = $("<option>");

                option.attr("value", episodesKey);
                option.append("episode " + episodesKey);

                selectEps.append(option);
            }

            if(selectEps.children().length === 1)
                selectEps.hide();
            else
                selectEps.show();
        });

        selectEps.change((e) =>
        {
            const episode = e.target.value;

            selectUrls.empty();

            if(e.target.value !== "")
            {
                for (const url of seasons[selectSeason.val()][episode])
                {
                    const option = $("<option>").attr("value", url);

                    option.append(url);
                    selectUrls.append(option);
                }
            }

            if(selectUrls.children().length === 0)
                selectUrls.hide();
            else
                selectUrls.show();

            selectUrls.change();
        });

        selectUrls.change((e) =>
        {
            $("#ep").attr("src", e.target.value);
        });

        selectSeason.change();
        selectSeason.show();
    }
})();