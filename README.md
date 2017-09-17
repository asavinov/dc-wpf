
     ____        _         ____                                     _
    |  _ \  __ _| |_ __ _ / ___| ___  __  __ __  __  __ _ __  _  __| |_ __ 
    | | | |/ _` | __/ _` | |    / _ \|  `´  |  `´  |/ _` |  \| |/ _  | '__/
    | |_| | (_| | || (_| | |___| (_| | |\/| | |\/| | (_| |   ' | (_| | |
    |____/ \__,_|\__\__,_|\____|\___/|_|  |_|_|  |_|\__,_|_|\__|\__,_|_|

	 I N T E G R A T E.          T R A N S F O R M.          A N A L Y Z E.

# ![](images/dc-logo.png "DataCommandr") DataCommandr: Integrate. Transform. Analyze.

*DataCommandr* is a self-service tool created with a single mission: radically simplifying and democratizing all kinds of operations with data. By making data manipulations easier for non-IT users, DataCommandr dramatically shortens the total time for data integration, transformation and analysis. Data in DataCommandr is represented as tables and columns which are two main objects of the application. A table is a collection of data records. A column represents a property (also referred to as an attribute or field). Each column takes values from some other table which is referred to as a type of this column. Every element in the mash up - table or column - has some definition in terms of already existing elements. And the task of the user is to create appropriate definitions for tables and columns. 

## Features 

DataCommander has the following distinguishing features: 

* DataCommandr is based on the [Concept-Oriented Model](http://conceptoriented.org) (COM) which is a unified model aimed at generalizing existing data models and data modeling techniques. 

* DataCommandr uses a column-oriented approach where the main unit of data is a column and every column has its own definition in terms of other columns. Data processing is also performed column-wise rather than table-wise in most other systems. 

* DataCommandr can be viewed as an analogue of classical spreadsheets where columns are defined in terms of other columns (in this or other tables) rather than cells being defined in terms of other cells. It evaluates data in columns by using dependencies derived from column definitions. It is similar to the functional paradigm because the whole model is represented as a number of column and table definitions. 

* DataCommandr on an in-memory data processing engine written in C#: http://bitbucket.org/conceptoriented/dce-csharp. The data processing engine has also a Java implementation: http://bitbucket.org/conceptoriented/dc-core

## Importing data

Before data can be manipulated, it has to be imported into the system. DataCommandr provides connectors to several storage types. For example, in order to load data from a text file it is necessary to choose the corresponding command and then select the file. After that, select the column to be imported as well as other import parameters. After finishing import, a new table will be shown in the workspace. 

![](images/data_import.png?raw=true "Importing data")

## Defining new tables

DataCommandr provides two basic operations for creating a new table: product and project.

### Table product

To define a new product-table it is necessary to specify one or more existing tables as well as an optional filter condition. The new table will contain all combinations of the specified source tables which satisfy the filter condition. This operation is frequently used for multidimensional analysis where the product table represents a multi-dimensional space while the source tables represent axes.
Product operation

![](images/table_product.png?raw=true "Table product")

### Table project

Project is an operation which creates a new table from all (unique) values produced by selected columns of an existing table. This operation can be used for finding all unique values in a table or grouping elements of the source table by extracting groups into a separate table.

![](images/table_project.png?raw=true "Table project")

## Defining new columns

Users of DataCommandr can add new columns to tables created the analytic mash-up. Each column has some definition and a column can collect data from other columns in the mash-up according to its definition. Depending on the definition the following columns can be created: 
* **Calculate** columns returning primitive values 
* **Link** columns returning records from other tables 
* **Aggregate** columns which aggregate data stored in other columns

### Calculate columns

The user of DataCommandr can define a new column which computes its value by using other columns of this same table. Formally, a new column is a function of other columns. For example, a new column TotalPrice can be computed as the current record price multiplied by the number of items.

![](images/column_calculate.png?raw=true "Calculate column")

### Link columns 

These columns return a record from another table represented as a combination of values. Such columns are used to create a link between two existing tables by describing a mapping between individual attributes. In terms of the relational model, they are analogous to foreign keys (FK) although there exist some significant conceptual differences. In particular, a link is defined as a separate column which can be used as any other column. Link columns are also used for describing complex mapping between tables as well as in the project operation where its output is stored in a new table. A link column is defined by defining a mapping between columns for the source table and the target table. This mapping is a number of assignments where the user selects some target column for a source column. 

![](images/column_link.png?raw=true "Link column")

### Aggregate columns

An aggregate column processes groups of records from another table by aggregating all values into one value which is stored. To define an aggregated column it is necessary to provide the following parameters: fact table stores records to be broken into groups for aggregation, grouping column specifies records from the fact table belonging to one group, measure column specifies the values to be aggregated, aggregate function specifies the way values are aggregated like sum or average. 

![](images/column_aggregate.png?raw=true "Aggregate column")

## More Information

More information about DataCommandr and the underlying data model can be found here: 

* More information information on all aspects of concept-oriented paradigm including the concept-oriented model and concept-oriented programming including publications can found here: 
    * http://www.conceptoriented.org
    * http://www.conceptoriented.com

* Alexandr Savinov is an author of DataCommander Engine Java library as well as the underlying concept-oriented model (COM): 
    * http://conceptoriented.org/savinov
    * https://www.researchgate.net/profile/Alexandr_Savinov

* Some papers about this approach: 
    * A. Savinov. DataCommandr: Column-Oriented Data Integration, Transformation and Analysis. Proc. IoTBD 2016, 339-347. https://www.researchgate.net/publication/301764506_DataCommandr_Column-Oriented_Data_Integration_Transformation_and_Analysis
    * A. Savinov. ConceptMix: Self-Service Analytical Data Integration based on the Concept-Oriented Model. A. Savinov. 3rd International Conference on Data Technologies and Applications (DATA 2014), 78-84. https://www.researchgate.net/publication/265301356_ConceptMix_Self-Service_Analytical_Data_Integration_based_on_the_Concept-Oriented_Model
