# CalculationTypesLoans

Install the package
Using dotnet cli

dotnet add package AutoSort.NetCore
Or package manager

Install-Package AutoSort.NetCore
How to use?
Set default sort attribute
When applying the sort but didn't pass parameter ordering it will take configuration of model.

Import the library
using NetCore.AutoSort;
Adding attibute to model properties
public Hero
{
    // this will be the last sort and ascending
    [Sort(2)]
    public int Id { get; set; }
    // this will be sort first one and descending
    [Sort(SortDirection.Descending)]
    public string Name { get; set; }
    // this will sort in second one ascending
    [Sort(1)]
    public string History { get; set; }
}
More example in AutoSort.Example

License
This project is licensed under the MIT License - see the LICENSE file for details
