{$reference CodeCompletion.dll}

uses CodeCompletion, System.IO;

var
  TestSuiteDir: string;

var
  PathSeparator: string := Path.DirectorySeparatorChar;

function GetTestSuiteDir: string;
begin
  var dir := Path.GetDirectoryName(GetEXEFileName());
  var ind := dir.LastIndexOf('bin');
  Result := dir.Substring(0, ind) + 'TestSuite';
end;

procedure RunRenameTests;
begin
  CodeCompletion.RenameTester
    .TestRename(TestSuiteDir + PathSeparator + 'rename_tests');
end;

begin
  try
    TestSuiteDir := GetTestSuiteDir;
    RunRenameTests;
    Writeln('Rename tests passed successfully.');
  except
    on e: Exception do
      Assert(false, e.ToString());
  end;
end.