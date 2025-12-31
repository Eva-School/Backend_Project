import 'package:flutter/material.dart';
import 'package:flutter_application_2/models/prodect_model.dart';
import 'package:flutter_application_2/widgets/builder_Card.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        centerTitle: true,
        title: Text("Product"),
        actions: [Icon(Icons.search)],
      ),
      body: GridView.builder(
        itemCount: ProdectList.length,
        gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
          crossAxisCount: 2,
        ),
        itemBuilder: (context, index) {
          return InkWell(
            child: BuilderCard(
              imgurl: ProdectList[index].Imgurl,
              Name: ProdectList[index].Name,
              Description: ProdectList[index].Description,
            ),
          );
        },
      ),
    );
  }
}
