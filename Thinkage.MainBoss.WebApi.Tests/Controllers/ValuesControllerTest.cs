using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Thinkage.MainBoss.WebApi;
using Thinkage.MainBoss.WebApi.Controllers;

namespace Thinkage.MainBoss.WebApi.Tests.Controllers
{
	public class ValuesController
	{
		public IEnumerable<string> Get()
		{
			return null;
		}
		public string Get(int id)
		{
			return null;
		}
		public void Post(string x)
		{
		}
		public void Put(int id, string x)
		{
		}
		public void Delete(int id)
		{
		}
	}
	[TestClass]
	public class ValuesControllerTest
	{
		[TestMethod]
		public void Get()
		{
			// Arrange
			ValuesController controller = new ValuesController();

			// Act
			IEnumerable<string> result = controller.Get();

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(2, result.Count());
			Assert.AreEqual("value1", result.ElementAt(0));
			Assert.AreEqual("value2", result.ElementAt(1));
		}

		[TestMethod]
		public void GetById()
		{
			// Arrange
			ValuesController controller = new ValuesController();

			// Act
			string result = controller.Get(5);

			// Assert
			Assert.AreEqual("value", result);
		}

		[TestMethod]
		public void Post()
		{
			// Arrange
			ValuesController controller = new ValuesController();

			// Act
			controller.Post("value");

			// Assert
		}

		[TestMethod]
		public void Put()
		{
			// Arrange
			ValuesController controller = new ValuesController();

			// Act
			controller.Put(5, "value");

			// Assert
		}

		[TestMethod]
		public void Delete()
		{
			// Arrange
			ValuesController controller = new ValuesController();

			// Act
			controller.Delete(5);

			// Assert
		}
	}
}
