using Arcanachnid.Bourse24;

var spider = new Nephila();
await spider.StartScraping();

if (spider.IsSaveData())
{
    await spider.SaveDatabase();
}

if (spider.IsSaveData())
{
    await spider.SaveDatabase();
}
