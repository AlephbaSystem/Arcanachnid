using Arcanachnid.Spiders.Majlis;

var spider = new Araneae();
await spider.StartScraping();

if (spider.IsSaveData())
{
    await spider.SaveDatabase();
}

if (spider.IsSaveData())
{
    await spider.SaveDatabase();
}