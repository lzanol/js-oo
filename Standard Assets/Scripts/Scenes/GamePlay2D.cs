using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSet;

public class GamePlay2D : GamePlayReal
{
	protected override void Awake()
	{
		base.Awake();

		NaturalOnly = true;
		AccidentOnly = true;
	}
}
