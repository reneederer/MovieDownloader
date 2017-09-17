namespace Episodes

open HtmlAgilityPack

open Aether
open Aether.Operators
open System
open System.Collections.Generic
open System.Linq
open System.Text
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System.Net.Http
open System.Net
open System.IO
open HtmlAgilityPack
open FSharp.Data
open System.Text.RegularExpressions
open FSharpx
open FSharpx.Option

module EpisodeListDownloader = 
    type SeriesJson = JsonProvider<""" { "seriesTitle": "Die Simpsons", "seasons": [ [ { "hosters": [ "https://serienstream.to/out/12/1696490", "https://serienstream.to/out/12/1696491" ], "description": "Es herrscht eine Umweltkrise in Springfield: Der See ist zugemüllt und der nächste Krümel Abfall, der in den See gekippt wird, verursacht eine Katastrophe.Homer nimmt das für einen Donut in Kauf und kippt ein Silo mit Schweinemist in den See.Die Regierung reagiert mit dem Einsperren der Stadt unter einer Kuppel.Nachdem herauskommt, dass Homer der Täter war, beginnt eine Jagd. Durch ganz Amerika bis hin nach Alaska und wieder zurück nach Springfield. Und alle sind am Ende glücklich.", "title": "Die Simpsons - Der Film" }, { "hosters": [ "https://serienstream.to/out/12/3833", "https://serienstream.to/out/12/2037815", "https://serienstream.to/out/12/1743926" ], "description": "Marge bringt Maggie in die Kita wo an Maggie zuerst Gehirntests unterzogen werden und sie dann in eine Gruppe mit Gerald gesteckt wird.", "title": "Die Simpsons: Der längste Kita Tag" } ], [ { "hosters": [ "https://serienstream.to/out/12/2254943", "https://serienstream.to/out/12/2254947", "https://serienstream.to/out/12/2254944", "https://serienstream.to/out/12/2254942", "https://serienstream.to/out/12/2254946", "https://serienstream.to/out/12/2254945", "https://serienstream.to/out/12/2042514" ], "description": "Weihnachten steht vor der Tür, und die Kleinen präsentieren Homer und Marge ihre Wunschzettel: Lisa wünscht sich ein Pony, Bart eine Tätowierung. Als Bart dann tatsächlich mit einer Tätowierung nach Hause kommt, geht für das Entfernen der Tätowierung das gesamte Weihnachtsgeld drauf. Inzwischen erfährt Homer, dass es diesmal keine Weihnachtsgratifikation gibt. So nimmt der geplagte Vater einen Nebenjob als Weihnachtsmann an!", "title": "Es Weihnachtet Schwer" }, { "hosters": [ "https://serienstream.to/out/12/2254951", "https://serienstream.to/out/12/2254949", "https://serienstream.to/out/12/2254950", "https://serienstream.to/out/12/2254952", "https://serienstream.to/out/12/2254953", "https://serienstream.to/out/12/2254948", "https://serienstream.to/out/12/2042515" ], "description": "Nachdem Bart am ende eines Testes einen Test von Martin nimmt und seinen Namen mit seinem Test tauscht, wird Bart von dem Schulpsychologen an eine andere Schule weitergeleitet in der nur Kinder mit einem hohen IQ unterrichtet werden. Doch diese merken schon bald das Bart nicht so schlau ist wie er im Test abgeschnitten hat.", "title": "Bart wird ein Genie" } ] ], "genres": [ "Zeichentrick", "Comedy" ] } """>
    type Episode = 
        { hosters : list<string>
        ; description : string
        ; title : string
        ; downloadFinished : bool
        }
        static member Hosters_ =
            (fun episode -> episode.hosters), (fun hosters episode -> { episode with hosters = hosters})
        static member Description_ =
            (fun episode -> episode.description), (fun description episode -> { episode with description = description})
        static member Title_ =
            (fun episode -> episode.title), (fun title episode -> { episode with title = title} : Episode)
        static member DownloadFinished_ =
            (fun episode -> episode.downloadFinished), (fun downloadFinished episode -> { episode with downloadFinished = downloadFinished})
    and Season = 
        { episodes : list<Episode>
        }
        static member Episodes_ =
            (fun season -> season.episodes), (fun episodes season -> { season with episodes = episodes })
            
    and Series = 
        { title : string
        ; seasons : list<Season>
        }
        static member Title_ =
            (fun series -> series.title), (fun title series -> { series with title = title } : Series)
        static member Seasons_ = 
            (fun series -> series.seasons), (fun seasons series -> { series with seasons = seasons } : Series)
        static member SesonsNr_ n =
            (fun series -> series.seasons.[n]), (fun seasons series -> { series with seasons = seasons } : Series)

    let readSeries = 
        let seriesJson = SeriesJson.Parse(File.ReadAllText("hosters_simpsons.json"))
        { title = seriesJson.SeriesTitle
        ; seasons =
            [ for currentSeason in seriesJson.Seasons do
                yield {
                    episodes =
                        [for currentEpisode in currentSeason do
                            yield {
                                hosters = currentEpisode.Hosters |> Array.toList
                                ; description = currentEpisode.Description
                                ; title = currentEpisode.Title
                                ; downloadFinished = false
                                }
                        ]
                    }
            ]
        }

    let dir = "/storage/sdcard/renesmovies"

    let setDownloadFinished series seasonNr episodeNr (finished : bool) =
        Optic.set (Series.Seasons_ >-> List.pos_ seasonNr >?> Season.Episodes_ >?> List.pos_ episodeNr >?> Episode.DownloadFinished_) true series
        
    let isDownloadFinished series seasonNr episodeNr : option<bool> =
        Optic.get (Series.Seasons_ >-> List.pos_ seasonNr >?> Season.Episodes_ >?> List.pos_ episodeNr >?> Episode.DownloadFinished_) series

    let seasonCount series =
        List.length series.seasons

    let episodeCount series seasonNr =
        try
            List.length series.seasons.[seasonNr].episodes
        with _ -> 0

    let writeDict (seriesTitle : string) dir series =
        File.WriteAllText(dir + "/" + "hosters_" + seriesTitle + ".json", JsonConvert.SerializeObject(series))

    let shouldDownloadSeason seasonNr (seasonsToDownload : list<int>) =
        let seasonContained = seasonsToDownload.Contains(seasonNr)
        let noSeasons = seasonsToDownload.Count() = 0
        seasonContained || noSeasons

    let shouldDownloadEpisode seasonNr (seasonsToDownload : list<int>) episodeNr (episodesToDownload : list<int>) =
        let episodeContained = episodesToDownload.Contains(episodeNr)
        let noEpisodes = episodesToDownload.Count() = 0
        shouldDownloadSeason seasonNr seasonsToDownload && (episodeContained || noEpisodes)

    let baseUrl = "https://serienstream.to/series"

    let addHoster series seasonNr episodeNr hosterUrl =
        let hosters = Series.Seasons_ >-> List.pos_ seasonNr >?> Season.Episodes_ >?> List.pos_ episodeNr >?> Episode.Hosters_
        Optic.set hosters (hosterUrl :: (Optic.get hosters series |> Option.getOrElse [])) series

    let getEpisode (episodeUrl : string)  =
        let doc = HtmlDocument.Load(episodeUrl)
        let hosterUrls =
            doc.Descendants("div").Where(fun el -> el.AttributeValue("class").Contains("hosterSiteVideo")).First().Descendants("a") .Select(fun el -> el.Attribute("href"));
        let pattern = @"Episode\s*(\d+)\s*Staffel\s*(\d+)\s*von\s*(.*?)\s*-\sSerienStream.to.*";
        let title = doc.Descendants("title").First().InnerText();
        let m = Regex.Match(title, pattern);
        let (episodeNr, readableSeriesTitle) = 
            if m.Success
            then
                let episodeNr = Int32.Parse(m.Groups.[1].Value) - 1
                let seasonNr = Int32.Parse(m.Groups.[2].Value)
                let readableSeriesTitle = m.Groups.[3].Value
                (episodeNr, readableSeriesTitle)
            else
                let pattern = "Film\s*(\d+)\s*Filme\s*von\s*(.*?)\s*-\sSerienStream.to.*"
                let m = Regex.Match(title, pattern)
                if m.Success
                then
                    let episodeNr = Int32.Parse(m.Groups.[1].Value) - 1
                    let seasonNr = 0
                    let readableSeriesTitle = m.Groups.[2].Value
                    (episodeNr, readableSeriesTitle)
                else (-1, "-")
        let episodeTitle = 
            try
                doc.Descendants("span").Where(fun el -> el.AttributeValue("class") = "episodeGermanTitel").First().InnerText()
            with _ ->
                try
                    doc.Descendants("span").Where(fun el -> el.AttributeValue("class") = "episodeEnglishTitel").First().InnerText()
                with _ -> "Kein Titel"
        let episodeDescription =
            try
                doc.Descendants("p").Where(fun el -> el.AttributeValue("class") = "descriptionSpoiler").First().InnerText()
            with _ -> ""
            

        let hosters =
            Seq.map
                (fun (hoster : HtmlAttribute) ->
                    (hoster.Value()))
                hosterUrls
            |> Seq.toList

        (
            { hosters = hosters
            ; description = episodeDescription
            ; title = episodeTitle
            ; downloadFinished = false
            }
            , episodeNr
        )

    let emptyEpisode =
        { title = ""
        ; description = ""
        ; hosters = []
        ; downloadFinished = false
        }

    let emptySeason = 
        { episodes = []
        }

    let emptySeries = 
        { title = ""
        ; seasons = []
        }

    let getSeason (seasonUrl : string) =
        let doc = HtmlDocument.Load(seasonUrl)
        let episodeUrls = doc.Elements("stream").First().Descendants("ul").ElementAt(1).Descendants("a")
        let seasonNr =
            let htmlTitle = doc.Elements("title").First().InnerText()
            let pattern = @"Staffel (\d*) .*"
            let m = Regex.Match(htmlTitle, pattern)
            if m.Success
            then Int32.Parse(m.Groups.[1].Value)
            else 0



        (
            { episodes =
                Seq.fold
                    (fun (state:list<Episode>) (episodeUrl : HtmlNode) ->
                        let (episode, episodeNr) = getEpisode (episodeUrl.AttributeValue("href"))
                        let l = state @ (List.replicate (Math.Max(0, episodeNr - (List.length state) + 1)) emptyEpisode)
                        List.take episodeNr l @ [episode] @ List.skip (episodeNr + 1) l
                    )
                    []
                    episodeUrls
            }
            , seasonNr
        )

            


    let downloadEpisodeList seriesTitle =
        let doc = HtmlDocument.Load(baseUrl + "/" + seriesTitle)
        let seasonUrls = doc.Elements("stream").First().Descendants("ul").First().Descendants("a");

        let seasons = 
            Seq.fold
                (fun state (seasonUrl : HtmlNode) ->
                    let (season, seasonNr) = getSeason (seasonUrl.AttributeValue("href"))
                    let l = state @ (List.replicate (Math.Max(0, seasonNr - (List.length state) + 1)) emptySeason)
                    List.take seasonNr l @ [season] @ List.skip (seasonNr + 1) l
                )
                []
                seasonUrls
        emptySeries


