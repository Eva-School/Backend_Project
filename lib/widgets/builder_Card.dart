import 'package:flutter/material.dart';

class BuilderCard extends StatelessWidget {
  const BuilderCard({super.key, required this.imgurl, required this.Name, required this.Description});
  final String imgurl;
  final String Name;
  final String Description;
  @override
  Widget build(BuildContext context) {
    return Card(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Image.asset(imgurl, height: 200, width: 150),
            Text(Name),
            Text(Description),
          ],
        ),
      );
  }
}