var path = args[0];

if (Path.GetExtension(path) != ".asm")
{
    await Console.Error.WriteLineAsync($"File path '{path}' does not have the .asm extension.");
    return ;
}

var fileInfo = new FileInfo(path);
if (!fileInfo.Exists)
{
    await Console.Error.WriteLineAsync($"File '{path}' does not exist.");
    return ;
}

var assemblerDirectory = fileInfo.Directory;
if (assemblerDirectory == null)
{
    throw new IOException($"File '{path}' existed but directory now does not.");
}

var fileLines = await File.ReadAllLinesAsync(path);


var predefinedSymbols = new Dictionary<string, int>
{
    { "SP", 0 },
    { "LCL", 1 },
    { "ARG", 2 },
    { "THIS", 3 },
    { "THAT", 4 },
    { "R0", 0 },
    { "R1", 1 },
    { "R2", 2 },
    { "R3", 3 },
    { "R4", 4 },
    { "R5", 5 },
    { "R6", 6 },
    { "R7", 7 },
    { "R8", 8 },
    { "R9", 9 },
    { "R10", 10 },
    { "R11", 11 },
    { "R12", 12 },
    { "R13", 13 },
    { "R14", 14 },
    { "R15", 15 },
    { "SCREEN", 16384 },
    { "KBD", 24576 }
};

const int baseRamAddress = 16;
var varSymbols = new Dictionary<string, int>();

var compComponents = new Dictionary<string, string>
{
    { "0", "0101010" },
    { "1", "0111111" },
    { "-1", "0111010" },
    { "D", "0001100" },
    { "A", "0110000" },
    { "M", "1110000" },
    { "!D", "0001101" },
    { "!A", "0110001" },
    { "!M", "1110001" },
    { "-D", "0001111" },
    { "-A", "0110011" },
    { "-M", "1110011" },
    { "D+1", "0011111" },
    { "A+1", "0110111" },
    { "M+1", "1110111" },
    { "D-1", "0001110" },
    { "A-1", "0110010" },
    { "M-1", "1110010" },
    { "D+A", "0000010" },
    { "D+M", "1000010" },
    { "D-A", "0010011" },
    { "D-M", "1010011" },
    { "A-D", "0000111" },
    { "M-D", "1000111" },
    { "D&A", "0000000" },
    { "D&M", "1000000" },
    { "D|A", "0101010" },
    { "D|M", "1101010" }
};

var destComponents = new Dictionary<string, string>
{
    { "", "000" },
    { "M", "001" },
    { "D", "010" },
    { "MD", "011" },
    { "A", "100" },
    { "AM", "101" },
    { "AD", "110" },
    { "AMD", "111" }
};

var jumpComponents = new Dictionary<string, string>
{
    { "", "000" },
    { "JGT", "001" },
    { "JEQ", "010" },
    { "JGE", "011" },
    { "JLT", "100" },
    { "JNE", "101" },
    { "JLE", "110" },
    { "JMP", "111" }
};

async Task WriteErrorAsync(int inputLineNumber, string originalLine, string message)
{
    await Console.Error.WriteLineAsync($"Line {inputLineNumber}: '{originalLine}': {message}");
}

var labels = new Dictionary<string, int>();
var output = new List<string>();
var nextOutputInstructionNumber = 0;
var inputLineNumber = 1;

foreach (var line in fileLines)
{
    var trimmedLine = line.Trim();
    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
    {
        inputLineNumber++;
    }
    else if (trimmedLine.StartsWith("("))
    {
        var label = trimmedLine.TrimStart('(').TrimEnd(')');
        if (labels.ContainsKey(label))
        {
            await WriteErrorAsync(inputLineNumber, line, "Label has been used more than once.");
            return ;
        }

        labels.Add(label, nextOutputInstructionNumber);
        inputLineNumber++;
    }
    else
    {
        nextOutputInstructionNumber++;
    }
}

foreach (var line in fileLines)
{
    var trimmedLine = line.Trim();
    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
    {
        inputLineNumber++;
    } 
    else if (trimmedLine.StartsWith("("))
    {
        // Already processed
    }
    else if (trimmedLine.StartsWith("@"))
    {
        // A-instruction
        var value = trimmedLine.TrimStart('@');
        
        if (uint.TryParse(value, out var uintValue))
        {
            if (uintValue > 32767)
            {
                await WriteErrorAsync(inputLineNumber, line, "Value is greater than 32767");
                return;
            }
            
            var binaryValue = Convert.ToString(uintValue, 2).PadLeft(16, '0');
            output.Add(binaryValue);
        }
        else if (decimal.TryParse(value, out var _))
        {
            await WriteErrorAsync(inputLineNumber, line, "Numerical value is not an integer between 0 and 32767 inc.");
            return;
        }
        else if(predefinedSymbols.ContainsKey(value))
        {
            var binaryValue = Convert.ToString(predefinedSymbols[value], 2).PadLeft(16, '0');
            output.Add(binaryValue);
        }
        else if(labels.ContainsKey(value))
        {
            var binaryValue = Convert.ToString(labels[value], 2).PadLeft(16, '0');
            output.Add(binaryValue);
        }
        else
        {
            if (!varSymbols.ContainsKey(value))
            {
                if (varSymbols.Count == 0)
                {
                    varSymbols.Add(value, baseRamAddress);
                }
                else
                {
                    var maxValue = varSymbols.Values.Max(v=>v);
                    varSymbols.Add(value, maxValue +1);
                }
            }
            
            var binaryValue = Convert.ToString(varSymbols[value], 2).PadLeft(16, '0');
            output.Add(binaryValue);
        }

        nextOutputInstructionNumber++;
        inputLineNumber++;
    }
    else
    {
        // C-instruction
        var rest = trimmedLine;
        var jump = "";
        var dest = "";
        var outputLine = "111";
        
        if (rest.Contains(";"))
        {
            var jumpStart = rest.IndexOf(';');
            jump = rest.Substring(jumpStart + 1);
            rest = rest.Substring(0, jumpStart);
        }
        
        if (rest.Contains("="))
        {
            var destStart = rest.IndexOf('=');
            dest = rest.Substring(0, destStart);
            rest = rest.Substring(destStart + 1);
        }

        var comp = rest;
        if (compComponents.ContainsKey(comp))
        {
            outputLine += compComponents[comp];
        }
        else
        {
            await WriteErrorAsync(inputLineNumber, line, "C-Instruction comp component not recognised.");
            return ;
        }
        
        if (destComponents.ContainsKey(dest))
        {
            outputLine += destComponents[dest];
        }
        else
        {
            await WriteErrorAsync(inputLineNumber, line, "C-Instruction dest component not recognised.");
            return ;
        }
        
        if (jumpComponents.ContainsKey(jump))
        {
            outputLine += jumpComponents[jump];
        }
        else
        {
            await WriteErrorAsync(inputLineNumber, line, "C-Instruction jump component not recognised.");
            return ;
        }

        output.Add(outputLine);
        
        nextOutputInstructionNumber++;
        inputLineNumber++;
    }
}

var outputFileInfo = new FileInfo(Path.Join(assemblerDirectory.FullName, Path.GetFileNameWithoutExtension(path) + ".hack"));
File.WriteAllLines(outputFileInfo.FullName, output);

Console.Write(outputFileInfo.FullName);