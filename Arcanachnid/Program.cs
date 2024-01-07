using Arcanachnid.VBulletin;

Stegodyphus barnamenevis = new Stegodyphus("https://barnamenevis.org");
var tmp = await barnamenevis.StartScraping("/");

_ = tmp;