//Sancia
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
namespace CF.WorkOrderProcess 
{
    public class WorkOrderLine : IPlugin
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
                trace.Trace("Work Order Line Plugin" + context.Depth);
                if ((context.MessageName.ToLower() == "create" || context.MessageName.ToLower() == "update") && context.InputParameters["Target"] is Entity && context.Depth < 5)
                {
                    

                    Entity  entity = (Entity)context.InputParameters["Target"];

                    Entity workOrderLine = service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));

                    UpdateWorkOrderLineCost(workOrderLine, service);

                    ReCalculateWorkOrderCost(service, workOrderLine);

				}
                else if(context.MessageName.ToLower() == "delete") {

					if (context.PreEntityImages.Contains("DeletedWorkOrderLine"))
					{
                        Entity preWorkOrderLinePart = (Entity)context.PreEntityImages["DeletedWorkOrderLine"];
                        if (preWorkOrderLinePart.Contains("rj_workorder"))
                        {
                            EntityReference workOrderRefrence = preWorkOrderLinePart.GetAttributeValue<EntityReference>("rj_workorder");
                            Entity workOrderLine = service.Retrieve(workOrderRefrence.LogicalName, workOrderRefrence.Id, new ColumnSet(true));
                            ReCalculateWorkOrderCost(service, workOrderLine);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error " + ex.Message);
            }
        }

       

        public static EntityCollection RetrieveWorkOrderLineData(Guid enityID, IOrganizationService service)
        {
            EntityCollection result = null;
            try
            {
                string fetchXml2 = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='rj_workordline'>
	                                <all-attributes/>
                                    <order attribute='rj_name' descending='false' />
                                    <filter type='and'>
                                        <condition attribute='rj_workorder' operator='eq' uitype='rj_workorder' value='" + enityID.ToString() + @"' />
                                      </filter>
                                  </entity>
                                </fetch>";


                result = service.RetrieveMultiple(new FetchExpression(fetchXml2));
            }
            catch (Exception ex)
            {
                throw new Exception("Error " + ex.Message);

            }
            return result;
        }

        private void UpdateWorkOrderLineCost(Entity workOrderLine, IOrganizationService service)
        {
            try
            {
                decimal labourCost = 0;
                decimal totalPartCost = 0;

				if (workOrderLine.Contains("rj_labourcost"))
				{
                    labourCost = workOrderLine.GetAttributeValue<Money>("rj_labourcost").Value;
				}
				if (workOrderLine.Contains("rj_partcost"))
				{
                    totalPartCost = workOrderLine.GetAttributeValue<Money>("rj_partcost").Value;
                }
                Entity updateTargetEntity = new Entity(workOrderLine.LogicalName);
                updateTargetEntity.Id = workOrderLine.Id;
                updateTargetEntity["rj_totalworkorderlinecost"] = new Money(labourCost + totalPartCost);
                service.Update(updateTargetEntity);

            }
            catch (Exception)
            {

                throw;
            }

        }
        //Cal of Work Order Line
        private static void ReCalculateWorkOrderCost(IOrganizationService service, Entity workOrderLine)
        {
			if (workOrderLine.Contains("rj_workorder"))
			{
                EntityReference workOrder = workOrderLine.GetAttributeValue<EntityReference>("rj_workorder");
                EntityCollection WorkOrderLineEntityCollection = RetrieveWorkOrderLineData(workOrder.Id, service);
                decimal totalWorkOrder = 0;
                if (WorkOrderLineEntityCollection.Entities.Count > 0)
                {
                    foreach (Entity currentWorkOrderLine in WorkOrderLineEntityCollection.Entities)
                    {
                        decimal totalLineCost = 0;
						if (currentWorkOrderLine.Contains("rj_totalworkorderlinecost"))
						{
                            totalLineCost = currentWorkOrderLine.GetAttributeValue<Money>("rj_totalworkorderlinecost").Value;
                            
                        }
                        totalWorkOrder += totalLineCost;
                    }
                }
                Entity updateTargetEntity = new Entity(workOrder.LogicalName);
                updateTargetEntity.Id = workOrder.Id;
                updateTargetEntity["rj_workordercost"] = new Money(totalWorkOrder);
                service.Update(updateTargetEntity);
            }
            

        }

    }
}

