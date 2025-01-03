﻿namespace imaluumFs.Application
open System.Net.Http
open AngleSharp.Html.Parser
open Microsoft.FSharp.Collections
open AngleSharp.Dom

type CourseInnerDetails = {Day:string;Time:string;Room:string;Instructor:string}
type CourseDetails = {Code:string;Section:string;Name:string;CreditHour:string;Details:CourseInnerDetails list}

module AlbiruniScraper =
    
    let scrapeTotalPages (document:IDocument) =
        let pageRow = document.QuerySelector("body > table:nth-child(4) > tbody > tr > td") |> _.QuerySelectorAll("*") |> List.ofSeq
        let pageRow = pageRow |> List.map (fun x-> x.InnerHtml)
        let result = match pageRow.Length with
                        | 0 -> []
                        | x when x > 2  && pageRow.Head = "PREV" -> pageRow.GetSlice(Some 1, Some (pageRow.Length - 2))
                        | _ when pageRow.Item(pageRow.Length - 1) = "NEXT" && pageRow.Head <> "PREV"-> pageRow.GetSlice(Some 0, Some (pageRow.Length - 2))
                        | _ -> pageRow
        result.Length
        
    let scrapeSubject (subject:IHtmlCollection<IElement>) =
        let code = subject.Item(0).InnerHtml
        let section= subject.Item(1).InnerHtml
        let name = subject.Item(2).InnerHtml
        let creditHour = subject.Item(3).InnerHtml
        let subjectDates =subject.Item(4).QuerySelectorAll("table > tbody > tr") |> List.ofSeq
        let subjectDates = subjectDates |> List.map(fun x -> x.QuerySelectorAll("td") |> List.ofSeq |> List.map(fun x -> x.InnerHtml) |> fun x-> {Day=x.Item(0);Time=x.Item(1);Room=x.Item(2);Instructor=x.Item(3)}) 
        {Name = name;Code =  code;CreditHour =  creditHour;Section =   section;Details =  subjectDates}
    let scrapeTable (document:IDocument) =
        let tables = document.QuerySelectorAll("body > table:nth-child(4) > tbody > tr") 
        let results = match tables with
                        | null -> Seq.empty
                        | _ -> Seq.rev tables |> Seq.tail |> fun x-> x |> Seq.removeAt (Seq.length x - 1) |> fun x-> x |> Seq.removeAt (Seq.length x - 1)
                                |> Seq.map(fun x -> x.QuerySelectorAll("td"))
                                |> Seq.map (fun x -> scrapeSubject x)
        results
        
    let scrapeWebsite (client:HttpClient) (baseUrl: string) (pageCount: int) (kuliyyah: string) (semester: string) (session: string) (ctype: string) = 
        let pageCount = match pageCount with
                        | 0 -> 1
                        | _ -> pageCount
        async {            
            let! results = 
                [1..pageCount]
                |> List.map (fun i ->
                    async {
                        let url = baseUrl + $"&view={50*i}" + kuliyyah + semester + session + ctype
                        try 
                            let! response = client.GetAsync(url) |> Async.AwaitTask
                            response.EnsureSuccessStatusCode() |> ignore
                            
                            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                            let document = HtmlParser().ParseDocument(content)
                            return scrapeTable document
                        with 
                        | ex -> 
                            printfn $"Error scraping {url}: {ex.Message}"
                            return []
                    }
                )
                |> Async.Parallel
            
            return results 
            |> Seq.collect id // Flatten results
            |> Seq.toList
        }
    
      