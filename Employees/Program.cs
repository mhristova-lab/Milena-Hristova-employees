if (args.Length < 1)
{
    Console.WriteLine("Please provide a path to a csv file");
    return 1;
}

return PrintBiggestOverlap();

int PrintBiggestOverlap()
{
    Dictionary<int, List<TimeRecord>> records = ReadCsvFile(args[0]);

    if (records.Count == 0)
    {
        return 1;
    }

    Dictionary<string, int> overlaps = SumOverlaps(records);

    int maxOverlap = 0;
    string maxOverlapKey = string.Empty;

    foreach (var overlap in overlaps)
    {
        if (overlap.Value > maxOverlap)
        {
            maxOverlap = overlap.Value;
            maxOverlapKey = overlap.Key;
        }
    }

    var employeeIds = maxOverlapKey.Split('-');

    Console.WriteLine($"{employeeIds[0]}, {employeeIds[1]}, {maxOverlap}");
    return 0;
}

Dictionary<int, List<TimeRecord>> ReadCsvFile(string filename)
{
    var records = new Dictionary<int, List<TimeRecord>>();

    if (!File.Exists(filename))
    {
        Console.WriteLine("File does not exist");
        return records;
    }

    using (var reader = new StreamReader(filename))
    {
        string? line = reader.ReadLine();
        if (line == null)
        {
            Console.WriteLine("File is empty");
            return records;
        }

        do
        {
            string[] values = line.Split(',');

            if (!int.TryParse(values[0].Trim(), null, out int employeeId) ||
                !int.TryParse(values[1].Trim(), null, out int projectId) ||
                !DateTime.TryParse(values[2].Trim(), null, out DateTime startDate))
            {
                continue;
            }

            if (!DateTime.TryParse(values[3].Trim(), null, out DateTime endDate))
            {
                if (values[3].Trim() == "NULL")
                {
                    endDate = DateTime.Now;
                }
                else
                {
                    continue;
                }
            }

            if (!records.TryAdd(projectId, new List<TimeRecord>()))
            {
                records[projectId].Add(new TimeRecord
                {
                    EmployeeId = employeeId,
                    ProjectId = projectId,
                    Time = startDate,
                    IsStart = true
                });

                records[projectId].Add(new TimeRecord
                {
                    EmployeeId = employeeId,
                    ProjectId = projectId,
                    Time = endDate,
                    IsStart = false
                });
            }

            line = reader.ReadLine();
        }
        while (line != null);
    }

    return records;
}


Dictionary<string, int> SumOverlaps(Dictionary<int, List<TimeRecord>> records)
{
    // sum overlaps for each employee pair
    Dictionary<string, int> employeeOverlaps = new();

    // compute overlaps for each project
    foreach (var projectRecords in records)
    {
        // order the records by time
        List<TimeRecord> orderedProjectRecords = projectRecords.Value.OrderBy(x => x.Time).ToList();

        // contains the start times of the employees still working on the project in the current iteration
        Dictionary<int, DateTime> currentEmployeeStartTimes = new();

        foreach (var record in orderedProjectRecords)
        {
            if (record.IsStart)
            {
                if (!currentEmployeeStartTimes.TryAdd(record.EmployeeId, record.Time))
                {
                    //Duplicate record for the same employee, same project and overlapping times
                    continue;
                }
            }
            else
            {
                if (!currentEmployeeStartTimes.TryGetValue(record.EmployeeId, out DateTime correspondingStart))
                {
                    continue;
                }

                var employeeIntervalDays = (record.Time - correspondingStart).Days;

                foreach (var employeeStartTime in currentEmployeeStartTimes)
                {
                    if (employeeStartTime.Key == record.EmployeeId)
                    {
                        continue;
                    }

                    var difference = (record.Time - employeeStartTime.Value).Days;

                    // the end of the overlapping interval is the current records time
                    // the start is either when the current employee started working on the project
                    // or when the other employee started working on the project
                    var overlappingDays = employeeIntervalDays < difference ? employeeIntervalDays : difference;

                    // key is the pair of employee ids in ascending order to avoid duplicates
                    var key = employeeStartTime.Key < record.EmployeeId ?
                        $"{employeeStartTime.Key}-{record.EmployeeId}" : $"{record.EmployeeId}-{employeeStartTime.Key}";

                    if (!employeeOverlaps.TryAdd(key, overlappingDays))
                    {
                        employeeOverlaps[key] += overlappingDays;
                    }
                }

                currentEmployeeStartTimes.Remove(record.EmployeeId);
            }
        }
    }

    return employeeOverlaps;
}

public record TimeRecord
{
    public int EmployeeId { get; set; }
    public int ProjectId { get; set; }
    public DateTime Time { get; set; }
    public bool IsStart { get; set; }
}