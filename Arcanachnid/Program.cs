using Arcanachnid.Bourse24;
using Arcanachnid.Nabzebourse;

var spider = new Nephila(batchMode: true);
await spider.StartScraping();

if (spider.IsSaveData())
{
    await spider.SaveDatabase();
}

if (spider.IsSaveData())
{
    await spider.SaveDatabase();
}

var spider2 = new Trichonephila(batchMode: true);
await spider2.StartScraping();

if (spider2.IsSaveData())
{
    await spider2.SaveDatabase();
}

if (spider2.IsSaveData())
{
    await spider2.SaveDatabase();
}