(() => {
	const live2d_path = "/lib/live/";

	function loadExternalResource(url, type) {
		return new Promise((resolve, reject) => {
			let tag;

			if (type === "css") {
				tag = document.createElement("link");
				tag.rel = "stylesheet";
				tag.href = url;
			}
			else if (type === "js") {
				tag = document.createElement("script");
				tag.src = url;
			}
			if (tag) {
				tag.onload = () => resolve(url);
				tag.onerror = () => reject(url);
				document.head.appendChild(tag);
			}
		});
	}

	if (screen.width >= 768)
	{
		Promise.all([
			loadExternalResource(live2d_path + "waifu.min.css", "css"),
			loadExternalResource(live2d_path + "live2d.min.js", "js"),
			loadExternalResource(live2d_path + "waifu-tips.js", "js")
		]).then(() => {
			LiveWidget.getLiveModel({
				waifuPath: live2d_path + "waifu-tips.json",
				cdnPath: "/live2dapi/"
			});
		});
	}

})();
