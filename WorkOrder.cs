using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CF.WorkOrderProcess
{
    public class WorkOrder : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            //obtain the tracing service: for logs
            ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            //Obtain the execution context from the service provider.
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            //For the current user who has logged in
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            try
            {
                trace.Trace("Work Order Plugin" + context.Depth);
                // The InputParameters collection contains all the data passed in the message request.            
                if ((context.MessageName.ToLower() == "create" || context.MessageName.ToLower() == "update") && context.InputParameters["Target"] is Entity && context.Depth < 5)
                {
                    Entity entity = new Entity();
                    entity = (Entity)context.InputParameters["Target"];

                    
                    Entity workOrder = service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));

					decimal tax = 0;
                    decimal wordOrderCost = 0;
                    if (workOrder.Contains("rj_tax"))
                    {
                        tax = workOrder.GetAttributeValue<Money>("rj_tax").Value;
                    }
					if (workOrder.Contains("rj_workordercost"))
					{
                        wordOrderCost = workOrder.GetAttributeValue<Money>("rj_workordercost").Value;
					}

                    Entity updateTargetEntity = new Entity(workOrder.LogicalName);
                    updateTargetEntity.Id = workOrder.Id;
                    updateTargetEntity["rj_totalcost"] = new Money(wordOrderCost + tax);
                    service.Update(updateTargetEntity);
                }


            }
            catch (Exception ex)
            {
                throw new Exception("Error" + ex.Message);
            }
        }
    }
}
