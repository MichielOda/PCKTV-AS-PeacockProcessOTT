/*
****************************************************************************
*  Copyright (c) 2022,  Skyline Communications NV  All Rights Reserved.    *
****************************************************************************

By using this script, you expressly agree with the usage terms and
conditions set out below.
This script and all related materials are protected by copyrights and
other intellectual property rights that exclusively belong
to Skyline Communications.

A user license granted for this script is strictly for personal use only.
This script may not be used in any way by anyone without the prior
written consent of Skyline Communications. Any sublicensing of this
script is forbidden.

Any modifications to this script by the user are only allowed for
personal use and within the intended purpose of the script,
and will remain the sole responsibility of the user.
Skyline Communications will not be responsible for any damages or
malfunctions whatsoever of the script resulting from a modification
or adaptation by the user.

The content of this script is confidential information.
The user hereby agrees to keep this confidential information strictly
secret and confidential and not to disclose or reveal it, in whole
or in part, directly or indirectly to any person, entity, organization
or administration without the prior written consent of
Skyline Communications.

Any inquiries can be addressed to:

    Skyline Communications NV
    Ambachtenstraat 33
    B-8870 Izegem
    Belgium
    Tel.    : +32 51 31 35 69
    Fax.    : +32 51 31 01 29
    E-mail  : info@skyline.be
    Web     : www.skyline.be
    Contact : Ben Vandenberghe

****************************************************************************
Revision History:

DATE        VERSION     AUTHOR          COMMENTS

dd/mm/2022  1.0.0.1     XXX, Skyline    Initial version
****************************************************************************
*/

using System;
using System.Diagnostics;
using System.Linq;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.DataMinerSolutions.ProcessAutomation.Helpers.Logging;
using Skyline.DataMiner.DataMinerSolutions.ProcessAutomation.Manager;
using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

/// <summary>
/// DataMiner Script Class.
/// </summary>
public class Script
{
    /// <summary>
    /// The Script entry point.
    /// </summary>
    /// <param name="engine">Link with SLAutomation process.</param>
    public void Run(Engine engine)
    {
        var helper = new PaProfileLoadDomHelper(engine);

        try
        {
            var subdomInstance = helper.GetParameterValue<Guid>("Conviva (Peacock)");
            var maindomInstance = helper.GetParameterValue<string>("InstanceId (Peacock)");
            var action = helper.GetParameterValue<string>("Action (Peacock)");
            var domHelper = new DomHelper(engine.SendSLNetMessages, "process_automation");
            engine.Log("Starting Conviva Subprocess");

            var subFilter = DomInstanceExposers.Id.Equal(new DomInstanceId(subdomInstance));
            var subInstances = domHelper.DomInstances.Read(subFilter);
            if (subInstances.Count == 0)
            {
                // returning success until conviva is ready
                helper.ReturnSuccess();
                return;
            }

            var subInstance = subInstances.First();

            var mainFilter = DomInstanceExposers.Id.Equal(new DomInstanceId(Guid.Parse(maindomInstance)));
            var mainInstance = domHelper.DomInstances.Read(mainFilter).First();
            engine.GenerateInformation("status of main process: " + mainInstance.StatusId);
            if (action == "provision")
            {
                domHelper.DomInstances.ExecuteAction(subInstance.ID, "provision");
            }
            else if (action == "reprovision")
            {
                domHelper.DomInstances.ExecuteAction(subInstance.ID, "reprovision");
            }
            else if (action == "deactivate")
            {
                domHelper.DomInstances.ExecuteAction(subInstance.ID, "deactivate");
            }
            else if (action == "complete-provision")
            {
                domHelper.DomInstances.ExecuteAction(subInstance.ID, "complete-provision");
            }

            if (mainInstance.StatusId == "ready")
            {
                helper.TransitionState("ready_to_inprogress");
            }
            else if (mainInstance.StatusId == "reprovision")
            {
                helper.TransitionState("reprovision_to_inprogress");
            }

            helper.ReturnSuccess();
        }
        catch (Exception ex)
        {
            engine.Log("Error: " + ex);
        }
    }
}