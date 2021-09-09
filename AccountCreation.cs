using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System.ServiceModel.Description;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Xrm.Tooling.Connector;
using CFS.AzureintegrationWorkOrder.DTO;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk.Query;

namespace CFS.AzureintegrationWorkOrder
{
    public static class AccountCreation
    {
        [FunctionName("AccountCreation")]
        public static async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = null)] HttpRequestMessage req, ILogger log)
        {
            var data = await req.Content.ReadAsStringAsync();
            Response response = JsonConvert.DeserializeObject<Response>(data);
            IOrganizationService service = Connect(log);
            HttpResponseMessage responseMessage = null;
            if (service != null)
            {
                try
                {
                    //Create a Account
                    Entity account = new Entity("account");
                    account["name"] = response.companyname;
                    Guid accountId = service.Create(account);

                    //Create entity account,contact
                    Entity contact = new Entity("contact");
                    contact["firstname"] = response.firstname;
                    contact["lastname"] = response.lastname;
                    contact["emailaddress1"] = response.email;
                    contact["parentcustomerid"] = new EntityReference("account", accountId);
                    Guid contactId = service.Create(contact);

                    Guid carId = carData(response, service);

                    //Create Work Order Entity
                    Entity workOrder = new Entity("rj_workorder");
                    workOrder["rj_car"] = new EntityReference("rj_car", carId);
                    workOrder["rj_account"] = new EntityReference("account", accountId);
                    workOrder["rj_contact"] = new EntityReference("contact", contactId);
                    Guid workOrderId = service.Create(workOrder);


                    //Create Work Order Line
                    Guid workorderline1Id = WorkOrderLineData(response.workorderline1, response.service1, response.labour1, service, workOrderId);
                    Guid workorderline2Id = WorkOrderLineData(response.workorderline2, response.service2, response.labour2, service, workOrderId);
                    Guid workorderline3Id = WorkOrderLineData(response.workorderline3, response.service3, response.labour3, service, workOrderId);


                    //Create a part
                    Guid workorderlinepart1Id = WorkOrderLinePart(response.part1, service, workOrderId, workorderline1Id);
                    Guid workorderlinepart2Id = WorkOrderLinePart(response.part2, service, workOrderId, workorderline2Id);
                    Guid workorderlinepart3Id = WorkOrderLinePart(response.part3, service, workOrderId, workorderline3Id);

                    responseMessage = req.CreateResponse(HttpStatusCode.Created, new HttpResponseMessage()
                    {
                        Content = new StringContent("Record is created")
                    });
                }
                catch (Exception ex)
                {
                    responseMessage = req.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
                }

            }
            return responseMessage;
        }

        private static Guid WorkOrderLinePart(string partName, IOrganizationService service,Guid workOrderId, Guid workOrderLineId)
        {
            string fetchXml1 = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='rj_part'>
	                                  <all-attributes/>
                                    <order attribute='rj_name' descending='false' />
                                    <filter type='and'>
                                      <condition attribute='rj_name' operator='eq' value='" + partName + @"' />
                                    </filter>
                                  </entity>
                                </fetch>";

            EntityCollection result1 = service.RetrieveMultiple(new FetchExpression(fetchXml1));
            //multiple records takes first record [0]
            Guid partId = result1.Entities[0].Id;

            //Create Work Order Line Part
            Entity workOrderLinePart = new Entity("rj_workorderlinepart");
            workOrderLinePart["rj_workorder"] = new EntityReference("rj_workorder", workOrderId);
            workOrderLinePart["rj_workorderlinelookup"] = new EntityReference("rj_workordline", workOrderLineId);
            workOrderLinePart["rj_name"] = partName;
            workOrderLinePart["rj_partlookups"] = new EntityReference("rj_part", partId);
            workOrderLinePart["rj_quantity"] = 1;
            Guid workorderlinepartId = service.Create(workOrderLinePart);
            return workorderlinepartId;

           
        }
      
        private static Guid WorkOrderLineData(string wOrderLineName, string serviceName, decimal labourCost, IOrganizationService service, Guid workorderId)
        {
            Entity workOrderLine = new Entity("rj_workordline");
            workOrderLine["rj_workorder"] = new EntityReference("rj_workorder", workorderId);
            workOrderLine["rj_name"] = wOrderLineName;
            workOrderLine["rj_servicetask"] = serviceName;
            workOrderLine["rj_labourcost"] = new Money(labourCost);
            Guid wOrderLineId = service.Create(workOrderLine);
            return wOrderLineId;
        }

        private static Guid carData(Response response, IOrganizationService service)
        {
            //Create entity car 
            //Entity car = new Entity("rj_car");
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='rj_car'>
	                                          <all-attributes/>
                                            <order attribute='rj_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='rj_name' operator='eq' value='" + response.car.ToString() + @"' />
                                            </filter>
                                          </entity>
                                        </fetch>";

            EntityCollection result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            //multiple records takes first record [0]
            Guid carId = result.Entities[0].Id;
            return carId;
        }

        public static IOrganizationService Connect(ILogger log)
        {
            CrmServiceClient svc = null;
            try
            {
                string userName = "rjacob@cfsrichie.onmicrosoft.com";
                string password = "Cloudfront$1";
                //string userName = "admin@cft007.onmicrosoft.com";
                //string password = "cloudfront$1";
                string authType = "OAuth";
                string url = "https://org68dcd75a.crm.dynamics.com/";
                string appId = "51f81489-12ee-4a9e-aaae-a2591f45987d";
                string reDirectURI = "app://58145B91-0C36-4500-8554-080854F2AC97";
                string loginPrompt = "Auto";
                string ConnectionString = string.Format("AuthType = {0};Username = {1};Password = {2}; Url = {3}; AppId={4}; RedirectUri={5};LoginPrompt={6}", authType, userName, password, url, appId, reDirectURI, loginPrompt);
                svc = new CrmServiceClient(ConnectionString);
                if (svc.IsReady)
                {
                    Guid userid = ((WhoAmIResponse)svc.Execute(new WhoAmIRequest())).UserId;
                    if (userid != Guid.Empty)
                    {
                        log.LogInformation("Connection Established Successfully...");
                    }
                }
                else
                {
                    log.LogInformation("Failed to Established Connection!!!");
                }



            }
            catch (Exception ex)
            {
                log.LogError("Exception caught - " + ex.Message);
            }
            return svc;
        }
    }
}

