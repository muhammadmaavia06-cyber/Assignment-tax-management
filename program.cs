using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ToolVehicleTaxSystem;

// =====================================================================
//  Tax payment status of a vehicle
// =====================================================================
public enum TaxStatus
{
    Pending,
    Paid,
    NotPaid
}

// =====================================================================
//  Base Class: ToolVehicle (abstract)
// =====================================================================
public abstract class ToolVehicle
{
    // ---------------- Static Properties (system-wide statistics) ----------------

    /// <summary>Total number of vehicles created.</summary>
    public static int TotalVehicles { get; private set; }

    /// <summary>Number of vehicles that have paid tax.</summary>
    public static int TotalTaxPayingVehicles { get; private set; }

    /// <summary>Number of vehicles that passed without paying tax.</summary>
    public static int TotalNonTaxPayingVehicles { get; private set; }

    /// <summary>Total amount of tax collected so far.</summary>
    public static decimal TotalTaxCollected { get; private set; }

    // ---------------- Instance Properties ----------------

    public int VehicleID { get; }
    public string RegNo { get; }
    public string Model { get; }
    public string Brand { get; }
    public decimal BasePrice { get; }
    public string VehicleType { get; }

    /// <summary>Tax status of this particular vehicle.</summary>
    public TaxStatus Status { get; private set; } = TaxStatus.Pending;

    /// <summary>Tax amount charged for this vehicle type.</summary>
    public abstract decimal TaxAmount { get; }

    // ---------------- Constructor ----------------

    /// <summary>
    /// Initializes the vehicle and increments the static TotalVehicles counter.
    /// Derived-class constructors call this one, so every vehicle is counted exactly once.
    /// </summary>
    protected ToolVehicle(string regNo, string model, string brand, decimal basePrice, string vehicleType)
    {
        if (string.IsNullOrWhiteSpace(regNo))
            throw new ArgumentException("Registration number cannot be empty.", nameof(regNo));
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model cannot be empty.", nameof(model));
        if (string.IsNullOrWhiteSpace(brand))
            throw new ArgumentException("Brand cannot be empty.", nameof(brand));
        if (basePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(basePrice), "Base price cannot be negative.");

        TotalVehicles++;
        VehicleID = TotalVehicles;
        RegNo = regNo.Trim().ToUpperInvariant();
        Model = model.Trim();
        Brand = brand.Trim();
        BasePrice = basePrice;
        VehicleType = vehicleType;
    }

    // ---------------- Methods ----------------

    /// <summary>
    /// Abstract method: each vehicle type handles its own tax payment.
    /// Returns true if the tax was paid, false if the vehicle was already processed.
    /// </summary>
    public abstract bool PayTax();

    /// <summary>
    /// Called when a vehicle passes the toll plaza without paying tax.
    /// Updates the static TotalNonTaxPayingVehicles property.
    /// Returns true on success, false if the vehicle was already processed.
    /// </summary>
    public bool PassWithoutPaying()
    {
        if (Status != TaxStatus.Pending)
        {
            Console.WriteLine($"  [!] Vehicle {RegNo} has already been processed (Status: {Status}).");
            return false;
        }

        Status = TaxStatus.NotPaid;
        TotalNonTaxPayingVehicles++;
        Console.WriteLine($"  [!] {VehicleType} {RegNo} passed WITHOUT paying tax.");
        return true;
    }

    /// <summary>
    /// Shared helper used by derived classes inside PayTax().
    /// Updates TotalTaxCollected and TotalTaxPayingVehicles safely.
    /// </summary>
    protected bool RecordTaxPayment(decimal amount)
    {
        if (Status != TaxStatus.Pending)
        {
            Console.WriteLine($"  [!] Vehicle {RegNo} has already been processed (Status: {Status}).");
            return false;
        }

        Status = TaxStatus.Paid;
        TotalTaxPayingVehicles++;
        TotalTaxCollected += amount;
        Console.WriteLine($"  [+] {VehicleType} {RegNo} paid tax of {amount:C}.");
        return true;
    }

    public override string ToString() =>
        $"{VehicleID,-4}{VehicleType,-14}{RegNo,-12}{Brand,-12}{Model,-12}{BasePrice,12:C}  {Status}";
}

// =====================================================================
//  Derived Class: Car (tax = $2)
// =====================================================================
public class Car : ToolVehicle
{
    public const decimal CarTax = 2m;

    public override decimal TaxAmount => CarTax;

    public Car(string regNo, string model, string brand, decimal basePrice)
        : base(regNo, model, brand, basePrice, "Car")
    {
    }

    public override bool PayTax() => RecordTaxPayment(TaxAmount);
}

// =====================================================================
//  Derived Class: Bike (tax = $1)
// =====================================================================
public class Bike : ToolVehicle
{
    public const decimal BikeTax = 1m;

    public override decimal TaxAmount => BikeTax;

    public Bike(string regNo, string model, string brand, decimal basePrice)
        : base(regNo, model, brand, basePrice, "Bike")
    {
    }

    public override bool PayTax() => RecordTaxPayment(TaxAmount);
}

// =====================================================================
//  Derived Class: HeavyVehicle (tax = $4)
// =====================================================================
public class HeavyVehicle : ToolVehicle
{
    public const decimal HeavyVehicleTax = 4m;

    public override decimal TaxAmount => HeavyVehicleTax;

    public HeavyVehicle(string regNo, string model, string brand, decimal basePrice)
        : base(regNo, model, brand, basePrice, "HeavyVehicle")
    {
    }

    public override bool PayTax() => RecordTaxPayment(TaxAmount);
}

// =====================================================================
//  Program: menu-driven console application
// =====================================================================
public class Program
{
    private static readonly List<ToolVehicle> Vehicles = new List<ToolVehicle>();

    public static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        bool running = true;
        while (running)
        {
            ShowMenu();
            string choice = ReadLine("Enter your choice: ");
            Console.WriteLine();

            switch (choice)
            {
                case "1": AddVehicle(); break;
                case "2": ProcessVehicle(pay: true); break;
                case "3": ProcessVehicle(pay: false); break;
                case "4": ListVehicles(); break;
                case "5": Console.WriteLine($"  Total Tax Collected: {ToolVehicle.TotalTaxCollected:C}"); break;
                case "6": Console.WriteLine($"  Total Vehicles: {ToolVehicle.TotalVehicles}"); break;
                case "7": Console.WriteLine($"  Tax Paying Vehicles: {ToolVehicle.TotalTaxPayingVehicles}"); break;
                case "8": Console.WriteLine($"  Non-Tax Paying Vehicles: {ToolVehicle.TotalNonTaxPayingVehicles}"); break;
                case "9": ShowFullReport(); break;
                case "10": LoadSampleData(); break;
                case "0": running = false; Console.WriteLine("  Thank you for using the system. Goodbye!"); break;
                default: Console.WriteLine("  [!] Invalid choice. Please select a number from the menu."); break;
            }

            if (running)
            {
                Console.WriteLine("\nPress Enter to continue...");
                Console.ReadLine();
            }
        }
    }

    // ---------------- Menu ----------------

    private static void ShowMenu()
    {
        Console.Clear();
        Console.WriteLine("==============================================");
        Console.WriteLine("     TOOL VEHICLE TAX MANAGEMENT SYSTEM");
        Console.WriteLine("==============================================");
        Console.WriteLine(" 1.  Add a new vehicle");
        Console.WriteLine(" 2.  Pay tax for a vehicle");
        Console.WriteLine(" 3.  Pass a vehicle without paying");
        Console.WriteLine(" 4.  View all vehicles");
        Console.WriteLine(" 5.  Report: Total tax collected");
        Console.WriteLine(" 6.  Report: Total vehicles");
        Console.WriteLine(" 7.  Report: Total tax paying vehicles");
        Console.WriteLine(" 8.  Report: Total non-tax paying vehicles");
        Console.WriteLine(" 9.  Full summary report");
        Console.WriteLine(" 10. Load sample data (demo)");
        Console.WriteLine(" 0.  Exit");
        Console.WriteLine("----------------------------------------------");
    }

    // ---------------- Actions ----------------

    private static void AddVehicle()
    {
        Console.WriteLine("Select vehicle type:");
        Console.WriteLine("  1. Car          (tax $2)");
        Console.WriteLine("  2. Bike         (tax $1)");
        Console.WriteLine("  3. HeavyVehicle (tax $4)");
        string type = ReadLine("Type: ");

        if (type != "1" && type != "2" && type != "3")
        {
            Console.WriteLine("  [!] Invalid vehicle type.");
            return;
        }

        string regNo = ReadNonEmpty("Registration No: ").ToUpperInvariant();
        if (FindByRegNo(regNo) != null)
        {
            Console.WriteLine($"  [!] A vehicle with registration number {regNo} already exists.");
            return;
        }

        string brand = ReadNonEmpty("Brand: ");
        string model = ReadNonEmpty("Model: ");
        decimal basePrice = ReadDecimal("Base Price: ");

        ToolVehicle vehicle = type switch
        {
            "1" => new Car(regNo, model, brand, basePrice),
            "2" => new Bike(regNo, model, brand, basePrice),
            _ => new HeavyVehicle(regNo, model, brand, basePrice)
        };

        Vehicles.Add(vehicle);
        Console.WriteLine($"  [+] {vehicle.VehicleType} added successfully with ID {vehicle.VehicleID}.");
    }

    private static void ProcessVehicle(bool pay)
    {
        if (Vehicles.Count == 0)
        {
            Console.WriteLine("  [!] No vehicles available. Please add a vehicle first.");
            return;
        }

        ListVehicles();
        int id = ReadInt("\nEnter Vehicle ID: ");
        ToolVehicle? vehicle = Vehicles.FirstOrDefault(v => v.VehicleID == id);

        if (vehicle == null)
        {
            Console.WriteLine("  [!] Vehicle not found.");
            return;
        }

        if (pay) vehicle.PayTax();
        else vehicle.PassWithoutPaying();
    }

    private static void ListVehicles()
    {
        if (Vehicles.Count == 0)
        {
            Console.WriteLine("  No vehicles registered yet.");
            return;
        }

        Console.WriteLine($"{"ID",-4}{"Type",-14}{"Reg No",-12}{"Brand",-12}{"Model",-12}{"Base Price",12}  Status");
        Console.WriteLine(new string('-', 75));
        foreach (ToolVehicle v in Vehicles)
            Console.WriteLine(v);
    }

    private static void ShowFullReport()
    {
        Console.WriteLine("========== TAX SUMMARY REPORT ==========");
        Console.WriteLine($"  Total Vehicles             : {ToolVehicle.TotalVehicles}");
        Console.WriteLine($"  Total Tax Paying Vehicles  : {ToolVehicle.TotalTaxPayingVehicles}");
        Console.WriteLine($"  Total Non-Tax Paying       : {ToolVehicle.TotalNonTaxPayingVehicles}");
        Console.WriteLine($"  Total Tax Collected        : {ToolVehicle.TotalTaxCollected:C}");
        Console.WriteLine("========================================");
    }

    private static void LoadSampleData()
    {
        var samples = new List<ToolVehicle>
        {
            new Car("ABC-123", "Corolla", "Toyota", 25000m),
            new Car("KHI-456", "Civic", "Honda", 28000m),
            new Bike("LHR-789", "CD70", "Honda", 1500m),
            new Bike("ISB-321", "Duke 200", "KTM", 4200m),
            new HeavyVehicle("TRK-001", "FH16", "Volvo", 90000m),
            new HeavyVehicle("BUS-002", "Citaro", "Mercedes", 120000m)
        };

        Vehicles.AddRange(samples);
        Console.WriteLine($"  [+] {samples.Count} sample vehicles loaded.\n");

        samples[0].PayTax();
        samples[1].PayTax();
        samples[2].PayTax();
        samples[3].PassWithoutPaying();
        samples[4].PayTax();
        samples[5].PassWithoutPaying();

        Console.WriteLine();
        ShowFullReport();
    }

    // ---------------- Helpers ----------------

    private static ToolVehicle? FindByRegNo(string regNo) =>
        Vehicles.FirstOrDefault(v => v.RegNo.Equals(regNo, StringComparison.OrdinalIgnoreCase));

    private static string ReadLine(string prompt)
    {
        Console.Write(prompt);
        return (Console.ReadLine() ?? string.Empty).Trim();
    }

    private static string ReadNonEmpty(string prompt)
    {
        while (true)
        {
            string input = ReadLine(prompt);
            if (!string.IsNullOrWhiteSpace(input)) return input;
            Console.WriteLine("  [!] Input cannot be empty. Try again.");
        }
    }

    private static int ReadInt(string prompt)
    {
        while (true)
        {
            if (int.TryParse(ReadLine(prompt), out int value)) return value;
            Console.WriteLine("  [!] Please enter a valid whole number.");
        }
    }

    private static decimal ReadDecimal(string prompt)
    {
        while (true)
        {
            if (decimal.TryParse(ReadLine(prompt), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) && value >= 0)
                return value;
            Console.WriteLine("  [!] Please enter a valid non-negative number.");
        }
    }
}
