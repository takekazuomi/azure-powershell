﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Internal.Resources.Models;
using Microsoft.Azure.Management.Internal.Network.Version2017_10_01.Models;
using System;
using Microsoft.Azure.Commands.Common.Strategies;

namespace Microsoft.Azure.Commands.Compute.Strategies.ComputeRp
{
    static class VirtualMachineStrategy
    {
        public static ResourceStrategy<VirtualMachine> Strategy { get; }
            = ComputePolicy.Create(
                provider: "virtualMachines",
                getOperations: client => client.VirtualMachines,
                getAsync: (o, p) => o.GetAsync(
                    p.ResourceGroupName, p.Name, null, p.CancellationToken),
                createOrUpdateAsync: (o, p) => o.CreateOrUpdateAsync(
                    p.ResourceGroupName, p.Name, p.Model, p.CancellationToken),
                createTime: c =>
                    c != null && c.OsProfile != null && c.OsProfile.WindowsConfiguration != null
                        ? 240
                        : 120);

        public static ResourceConfig<VirtualMachine> CreateVirtualMachineConfig(
            this ResourceConfig<ResourceGroup> resourceGroup,
            string name,
            ResourceConfig<NetworkInterface> networkInterface,
            Func<ImageAndOsType> getImageAndOsType,
            string adminUsername,
            string adminPassword,
            string size,
            ResourceConfig<AvailabilitySet> availabilitySet)
            => Strategy.CreateResourceConfig(
                resourceGroup: resourceGroup,
                name: name,
                createModel: engine =>
                {
                    var imageAndOsType = getImageAndOsType();
                    return new VirtualMachine
                    {
                        OsProfile = new OSProfile
                        {
                            ComputerName = name,
                            WindowsConfiguration = imageAndOsType.OsType == OperatingSystemTypes.Windows
                                ? new WindowsConfiguration()
                                : null,
                            LinuxConfiguration = imageAndOsType.OsType == OperatingSystemTypes.Linux 
                                ? new LinuxConfiguration()
                                : null,
                            AdminUsername = adminUsername,
                            AdminPassword = adminPassword,
                        },
                        NetworkProfile = new NetworkProfile
                        {
                            NetworkInterfaces = new[]
                            {
                                new NetworkInterfaceReference
                                {
                                    Id = engine.GetId(networkInterface)
                                }
                            }
                        },
                        HardwareProfile = new HardwareProfile
                        {
                            VmSize = size
                        },
                        StorageProfile = new StorageProfile
                        {
                            ImageReference = imageAndOsType.Image
                        },
                        AvailabilitySet = availabilitySet == null
                            ? null
                            : new Azure.Management.Compute.Models.SubResource
                            {
                                Id = engine.GetId(availabilitySet)
                            }
                    };
                });

        public static ResourceConfig<VirtualMachine> CreateVirtualMachineConfig(
            this ResourceConfig<ResourceGroup> resourceGroup,
            string name,
            ResourceConfig<NetworkInterface> networkInterface,
            OperatingSystemTypes osType,
            ResourceConfig<Disk> disk,
            string size,
            ResourceConfig<AvailabilitySet> availabilitySet)
            => Strategy.CreateResourceConfig(
                resourceGroup: resourceGroup,
                name: name,
                createModel: engine => new VirtualMachine
                {
                    OsProfile = null,
                    NetworkProfile = new NetworkProfile
                    {
                        NetworkInterfaces = new[]
                        {
                            new NetworkInterfaceReference
                            {
                                Id = engine.GetId(networkInterface)
                            }
                        }
                    },
                    HardwareProfile = new HardwareProfile
                    {
                        VmSize = size
                    },
                    StorageProfile = new StorageProfile
                    {
                        OsDisk = new OSDisk
                        {
                            Name = disk.Name,
                            CreateOption = DiskCreateOptionTypes.Attach,
                            OsType = osType,
                            ManagedDisk = new ManagedDiskParameters
                            {
                                StorageAccountType = StorageAccountTypes.PremiumLRS,
                                Id = engine.GetId(disk)
                            }
                        }
                    },
                    AvailabilitySet = availabilitySet == null
                        ? null
                        : new Azure.Management.Compute.Models.SubResource
                        {
                            Id = engine.GetId(availabilitySet)
                        }
                });
    }
}