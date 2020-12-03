# DIDA-GSTORE - Delivery 1 - Base Version

## Run a Project

Open project at the root folder and type
`dotnet run`

## Test with scripts

1. Open 4 terminal windows on PCS project and run on each terminal `dotnet run`
2. Tell the terminal PCS the port number to be used by it (start from 10000)
2. Open 1 terminal window on the root of the Delivery 1 - Base Version and run the Puppet Master with the following command
   - `dotnet run --project PuppetMaster`
3. Test the script working by executing the script on the PuppetMaster terminal window by introducing the the path to the script file
   - `./scripts/sample_pm_script.txt`
