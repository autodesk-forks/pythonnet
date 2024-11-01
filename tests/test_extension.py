# -*- coding: utf-8 -*-

"""Test CLR extension method support."""

import Python.Test as Test
import pytest

def test_linq_extensions():
    """Test LINQ extensions."""
    import clr
    from System import String, Func
    from System.Collections.Generic import List
    from System import Linq
    clr.ImportExtensions(Linq)
    list = List[String]()
    list.Add('hello')
    list.Add('beautiful')
    list.Add('world')
    assert list.First() == 'hello'
    assert list.First[String]() == 'hello'
    assert list.Skip(1).First() == 'beautiful'
    assert list.FirstOrDefault(Func[String, bool](lambda x: x.startswith("w"))) == 'world'

def test_implement_generic_interface():
    import clr
    from Python.Test import NumberList
    from System import Linq
    clr.ImportExtensions(Linq)
    list = NumberList()
    assert list.Last() == 3

def test_datatable_extensions():
    import sys
    import clr

    clr.AddReference("System.Data")
    clr.AddReference("System.Data.Common")
    from System.Data import DataTable, DataTableExtensions
    clr.ImportExtensions(DataTableExtensions)
    dt = DataTable("test")
