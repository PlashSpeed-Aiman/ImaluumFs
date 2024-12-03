namespace WebApplication3.Controllers

open System
open System.Net.Http
open System.Reactive.Subjects
open FSharp.Control.Reactive
open AngleSharp.Html.Parser
open Microsoft.AspNetCore.Http.HttpResults
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open imaluumFs.Application


[<ApiController>]
[<Route("[controller]")>]
type AlbiruniController(logger : ILogger<AlbiruniController>) =
    inherit ControllerBase()
    

    [<HttpGet>]
    member this.Albiruni(link : string,kuliyyah:string,semester:string,session:string) : IActionResult =
            let res = async{ 
                let baseUrl = "https://myapps.iium.edu.my/StudentOnline/schedule1.php?action=view"
                let view ="&view=50"
                let kuliyyah = $"&kuly={kuliyyah}"
                let semester = $"&sem={semester}"
                let session = $"&ses={session}"
                let ctype = $"&ctype=<"
                
                let url = baseUrl + view + kuliyyah + semester + session + ctype
                let task = task{
                    let! res = new HttpClient() |> fun x-> x.GetAsync (url) 
                    let! file = res.Content.ReadAsStringAsync()
                    return file
                }
                let! res =  task |> Async.AwaitTask
                
                let document = HtmlParser().ParseDocument(res)

                let pageCount = AlbiruniScraper.scrapeTotalPages document
                let! results = AlbiruniScraper.scrapeWebsite (new HttpClient()) baseUrl pageCount kuliyyah semester session ctype
                return results
            }
            res |> Async.RunSynchronously |> fun x-> this.Ok(x)
        
    [<HttpGet;Route("page-count")>]
    member this.PageCount(link : string) : IActionResult =
        let res =async{ 
            let task = task{
                let! res = new HttpClient() |> fun x-> x.GetAsync (link)
                let file = res.Content.ReadAsStringAsync()
                return! file
            }
            let! stringAsync = task |> Async.AwaitTask
            let document = HtmlParser().ParseDocument(stringAsync)
            return AlbiruniScraper.scrapeTotalPages document
        }
        res |> Async.RunSynchronously |> fun x-> this.Ok(x)
           