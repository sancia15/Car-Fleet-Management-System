using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CF.WorkOrderProcess
{
    public class WorkOrderLinePart : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            //obtain the tracing service: for logs
            ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            //this.tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            //Obtain the execution context from the service provider.
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            //For the current user who has logged in
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            try
            {
                trace.Trace("Work Order Line Part Plugin" + context.Depth);
                if ((context.MessageName.ToLower() == "create" || context.MessageName.ToLower() == "update") && context.InputParameters["Target"] is Entity && context.Depth < 5)
				{
					Entity entity = new Entity();
					//Target is a type of entity used to access the data of the entity
					entity = (Entity)context.InputParameters["Target"];
					//Retrieve Updated Work Order Line part  Records
					Entity workOrderLinePartEntity = service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));

                    // work order line part ID(lookup id of work order line part that refernces work order line part to work order line
                    if (workOrderLinePartEntity.Contains("rj_workorderlinelookup")) {
                        Guid workOrderLineID = workOrderLinePartEntity.GetAttributeValue<EntityReference>("rj_workorderlinelookup").Id;
                        //  Retrieve all the work order line records
                        Entity workOrderLine = service.Retrieve("rj_workordline", workOrderLineID, new ColumnSet(true));
                        UpdateWorkOrderLinePartCost(workOrderLinePartEntity, service);
                        ReCalulateWorkOrderLineCost(service, workOrderLine);

                    }
				}
				else if(context.MessageName.ToLower() == "delete") {

					if (context.PreEntityImages.Contains("DeletedWorkOrderLinePart"))
					{
                        Entity preWorkOrderLinePart = (Entity)context.PreEntityImages["DeletedWorkOrderLinePart"];
                        if (preWorkOrderLinePart.Contains("rj_workorderlinelookup"))
                        {
                            EntityReference workOrderLineRefrence = preWorkOrderLinePart.GetAttributeValue<EntityReference>("rj_workorderlinelookup");
                            Entity workOrderLine = service.Retrieve(workOrderLineRefrence.LogicalName, workOrderLineRefrence.Id, new ColumnSet(true));
                            ReCalulateWorkOrderLineCost(service, workOrderLine);
                        }
                    }
                    
                }


            }
            catch (Exception ex)
            {

                throw;
            }



        }

		private void ReCalulateWorkOrderLineCost(IOrganizationService service,Entity workOrderLine)
		{
			//tracingService.Trace("workorderline" + workorderline);
			EntityCollection entityCollection = RetrieveWorkOrderLinePart(workOrderLine.Id, service);

            decimal totalPartCost = 0;
            Money labourCost = new Money(0);

            if (workOrderLine.Contains("rj_labourcost"))
            {
                labourCost = workOrderLine.GetAttributeValue<Money>("rj_labourcost");
            }

            if (entityCollection.Entities.Count > 0)
			{
				foreach (Entity currentWorkOrderLinePart in entityCollection.Entities)
				{
					
					if (currentWorkOrderLinePart.Contains("rj_totalpartcost"))
					{
						Money partCost = currentWorkOrderLinePart.GetAttributeValue<Money>("rj_totalpartcost");
                        totalPartCost += partCost.Value;
					}

				}
			}
			
			Entity updateTargetEntity = new Entity(workOrderLine.LogicalName);
			updateTargetEntity.Id = workOrderLine.Id;
			updateTargetEntity["rj_partcost"] = new Money(totalPartCost);
            updateTargetEntity["rj_totalworkorderlinecost"] = new Money(totalPartCost + labourCost.Value);
			service.Update(updateTargetEntity);
		}

		//Calculation of each part 
		private void UpdateWorkOrderLinePartCost(Entity workOrderLinepart, IOrganizationService service)
        {
            try
            {
				if (workOrderLinepart.Contains("rj_quantity"))
				{

                    int quantity = workOrderLinepart.GetAttributeValue<int>("rj_quantity");
                    if (workOrderLinepart.Contains("rj_partlookups")) {
                        Guid partID = workOrderLinepart.GetAttributeValue<EntityReference>("rj_partlookups").Id;
                        Entity partEntity = service.Retrieve("rj_part", partID, new ColumnSet(true));                
                        Money partUnitCost = partEntity.GetAttributeValue<Money>("rj_cost");
                        decimal totalPartCost = partUnitCost.Value * quantity;
                        Entity updateTargetEntity = new Entity(workOrderLinepart.LogicalName);
                        updateTargetEntity.Id = workOrderLinepart.Id;
                        updateTargetEntity["rj_totalpartcost"] = new Money(totalPartCost);
                        service.Update(updateTargetEntity);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

        }
        private EntityCollection RetrieveWorkOrderLinePart(Guid enityID, IOrganizationService service)
        {
            EntityCollection result = null;
            try
            {
                string fetchXml1 = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='rj_workorderlinepart'>
                                                       <all-attributes/>
                                                        <order attribute='rj_name' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='rj_workorderlinelookup' operator='eq' uitype='rj_workordline' value='" + enityID.ToString() + @"' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>";


                result = service.RetrieveMultiple(new FetchExpression(fetchXml1));

            }
            catch (Exception)
            {
                throw;
            }
            return result;

        }
    }
}

