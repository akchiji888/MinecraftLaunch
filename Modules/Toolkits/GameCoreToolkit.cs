using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MinecraftLaunch.Modules.Models.Launch;
using MinecraftLaunch.Modules.Parser;

namespace MinecraftLaunch.Modules.Toolkits;

public class GameCoreToolkit
{
	public DirectoryInfo Root { get; private set; }

	public List<(string, Exception)> ErrorGameCores { get; private set; }

	public GameCore GameCoreNameChange(string oldid, string newid)
	{
		FileInfo gamejson = new FileInfo(Path.Combine(Root.FullName, "versions", oldid, oldid + ".json"));
		FileInfo gameJar = new FileInfo(Path.Combine(Root.FullName, "versions", oldid, oldid + ".jar"));
		DirectoryInfo gameFolder = new DirectoryInfo(Path.Combine(Root.FullName, "versions", oldid));
		GameCoreJsonEntity entity = new GameCoreJsonEntity();
		try
		{
			entity = entity.ToJsonEntity(File.ReadAllText(gamejson.FullName));
			entity.Id = newid;
			foreach (GameCore i in GetGameCores(Root.FullName).ToList())
			{
				if (i.InheritsFrom == oldid)
				{
					i.InheritsFrom = newid;
					File.WriteAllText(Path.Combine(i.Root?.FullName, "versions", i.Id, i.Id + ".json"), GetGameCoreJsonEntity(Root.FullName, i.Id, i.InheritsFrom).ToJson());
				}
			}
			File.WriteAllText(gamejson.FullName, entity.ToJson());
			File.Move(gameJar.FullName, Path.Combine(Root.FullName, "versions", oldid, newid + ".jar"));
			File.Move(gamejson.FullName, Path.Combine(Root.FullName, "versions", oldid, newid + ".json"));
			Directory.Move(gameFolder.FullName, Path.Combine(Root.FullName, "versions", newid));
		}
		catch
		{
		}
		return GetGameCore(newid);
	}

	public GameCore GetGameCore(string id)
	{
		foreach (GameCore core in GetGameCores())
		{
			if (core.Id == id)
			{
				return core;
			}
		}
		return null;
	}

	public void Delete(string Id)
	{
		DirectoryInfo directory = new DirectoryInfo(Path.Combine(Root.FullName, "versions", Id));
		if (directory.Exists)
		{
			directory.DeleteAllFiles();
		}
		directory.Delete();
	}

	public IEnumerable<GameCore> GetGameCores()
	{
		List<GameCoreJsonEntity> entities = new List<GameCoreJsonEntity>();
		DirectoryInfo versionsFolder = new DirectoryInfo(Path.Combine(Root.FullName, "versions"));
		if (!versionsFolder.Exists)
		{
			versionsFolder.Create();
			return Array.Empty<GameCore>();
		}
		DirectoryInfo[] directories = versionsFolder.GetDirectories();
		foreach (DirectoryInfo item in directories)
		{
			FileInfo[] files2 = item.GetFiles();
			foreach (FileInfo files in files2)
			{
				if (files.Name == item.Name + ".json")
				{
					GameCoreJsonEntity entity = new GameCoreJsonEntity();
					try
					{
						entity = entity.ToJsonEntity(File.ReadAllText(files.FullName));
						entities.Add(entity);
					}
					catch
					{}					
				}
			}
		}
		GameCoreParser parser = new GameCoreParser(Root, entities);
		IEnumerable<GameCore> gameCores = parser.GetGameCores();
		ErrorGameCores = parser.ErrorGameCores;
		return gameCores;
	}

	public IEnumerable<GameCore> GameCoreScearh(string text) { 
		var gameCores = GetGameCores();
		var endCores = new List<GameCore>();

		var firstScearh = gameCores.Where(x => x.Id!.Contains(text));
		if (!firstScearh.Any()) {
			endCores.AddRange(SpotLightScearh(text));
		}

		return endCores;
	}

	public static GameCore GameCoreNameChange(string root, string oldid, string newid)
	{
		FileInfo gamejson = new FileInfo(Path.Combine(root, "versions", oldid, oldid + ".json"));
		FileInfo gameJar = new FileInfo(Path.Combine(root, "versions", oldid, oldid + ".jar"));
		DirectoryInfo gameFolder = new DirectoryInfo(Path.Combine(root, "versions", oldid));
		GameCoreJsonEntity entity = new GameCoreJsonEntity();
		try
		{
			entity = entity.ToJsonEntity(File.ReadAllText(gamejson.FullName));
			entity.Id = newid;
			foreach (GameCore i in GetGameCores(root).ToList())
			{
				if (i.InheritsFrom == oldid)
				{
					i.InheritsFrom = newid;
					File.WriteAllText(Path.Combine(i.Root?.FullName, "versions", i.Id, i.Id + ".json"), GetGameCoreJsonEntity(root, i.Id, i.InheritsFrom).ToJson());
				}
			}
			File.WriteAllText(gamejson.FullName, entity.ToJson());
			File.Move(gameJar.FullName, Path.Combine(root, "versions", oldid, newid + ".jar"));
			File.Move(gamejson.FullName, Path.Combine(root, "versions", oldid, newid + ".json"));
			Directory.Move(gameFolder.FullName, Path.Combine(root, "versions", newid));
		}
		catch
		{
		}
		return GetGameCore(root, newid);
	}

	public static GameCore GetGameCore(string root, string id)
	{
		if (string.IsNullOrEmpty(root))
		{
			foreach (GameCore core2 in GetGameCores(".minecraft"))
			{
				if (core2.Id == id)
				{
					return core2;
				}
			}
		}
		foreach (GameCore core in GetGameCores(root))
		{
			if (core.Id == id)
			{
				return core;
			}
		}
		return null;
	}

	public static void Delete(string root, string Id)
	{
		DirectoryInfo directory = new DirectoryInfo(Path.Combine(root, "versions", Id));
		if (directory.Exists)
		{
			directory.DeleteAllFiles();
		}
		directory.Delete();
	}

	public static IEnumerable<GameCore> GetGameCores(string root)
	{
		List<GameCoreJsonEntity> entities = new List<GameCoreJsonEntity>();
		DirectoryInfo versionsFolder = new DirectoryInfo(Path.Combine(root, "versions"));
		if (!versionsFolder.Exists)
		{
			versionsFolder.Create();
			return Array.Empty<GameCore>();
		}
		DirectoryInfo[] directories = versionsFolder.GetDirectories();
		foreach (DirectoryInfo item in directories)
		{
			FileInfo[] files2 = item.GetFiles();
			foreach (FileInfo files in files2)
			{
				if (files.Name == item.Name + ".json")
				{
					GameCoreJsonEntity entity = new GameCoreJsonEntity();
					try
					{
						entity = entity.ToJsonEntity(File.ReadAllText(files.FullName));
						entities.Add(entity);
					}
					catch
					{
					}
				}
			}
		}
		return new GameCoreParser(new DirectoryInfo(root), entities).GetGameCores();
	}

	internal IEnumerable<GameCore> SpotLightScearh(string text) {
		if (true) {		
			
		}

		return null;
	}

	internal static GameCoreJsonEntity GetGameCoreJsonEntity(string root, string id, string inheritsfrom)
	{
		new List<GameCoreJsonEntity>();
		DirectoryInfo versionsFolder = new DirectoryInfo(Path.Combine(root, "versions"));
		if (!versionsFolder.Exists)
		{
			versionsFolder.Create();
			return null;
		}
		DirectoryInfo[] directories = versionsFolder.GetDirectories();
		foreach (DirectoryInfo item in directories)
		{
			FileInfo[] files2 = item.GetFiles();
			foreach (FileInfo files in files2)
			{
				if (files.Name == item.Name + ".json")
				{
					GameCoreJsonEntity entity = new GameCoreJsonEntity();
					entity = entity.ToJsonEntity(File.ReadAllText(files.FullName));
					if (entity.Id == id)
					{
						entity.InheritsFrom = inheritsfrom;
						return entity;
					}
				}
			}
		}
		return null;
	}

	public GameCoreToolkit()
	{
		Root = new DirectoryInfo(".minecraft");
	}

	public GameCoreToolkit(string path)
	{
		Root = new DirectoryInfo(path);
	}

	public GameCoreToolkit(DirectoryInfo root)
	{
		Root = root;
	}

	public static implicit operator GameCoreToolkit(string path) => new GameCoreToolkit(path);

    public static implicit operator GameCoreToolkit(DirectoryInfo path) => new GameCoreToolkit(path);
}
