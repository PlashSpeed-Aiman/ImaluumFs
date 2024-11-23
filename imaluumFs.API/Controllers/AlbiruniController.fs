namespace WebApplication3.Controllers

open System.Net.Http
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
                let view ="&view=100"
                let kuliyyah = $"&kuly={kuliyyah}"
                let semester = $"&sem={semester}"
                let session = $"&ses={session}"
                let ctype = $"&ctype=<"
                let url = baseUrl + view + kuliyyah + semester + session + ctype
                 
                let task = task{
                    let! res = new HttpClient() |> fun x-> x.GetAsync (url) |> Async.AwaitTask
                    let! file = res.Content.ReadAsStringAsync()|> Async.AwaitTask
                    return file
                }
                let! res =  task |> Async.AwaitTask
                
                let document = HtmlParser().ParseDocument(res)
                return AlbiruniScraper.scrapeTable document 
            }
            res |> Async.RunSynchronously |> fun x->  match x with
                                                        | Some x -> this.Ok(x)
                                                        | None -> this.NotFound()   
        
    [<HttpGet;Route("page-count")>]
    member this.PageCount(link : string) : IActionResult =
        let task = task{
            let! res = new HttpClient() |> fun x-> x.GetAsync (link)
            let file = res.Content.ReadAsStringAsync()
            return! file
        }
       
        let stringAsync = task |> Async.AwaitTask
      
        let document = HtmlParser().ParseDocument(task.Result)
        AlbiruniScraper.scrapeTotalPages document |> fun x -> this.Ok(x)
           
