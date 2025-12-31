import 'package:flutter/material.dart';

class TextFormCustom extends StatelessWidget {
  const TextFormCustom({
    super.key,
    required this.isObscure,
    required this.hintText,
    this.suffixIcon,
    this.validator,
  });

  final bool isObscure;
  final String hintText;
  final Widget? suffixIcon;
  final String? Function(String?)? validator;
  @override
  Widget build(BuildContext context) {
    return TextFormField(
      validator: validator,
      obscureText: isObscure,
      decoration: InputDecoration(
        suffixIcon: suffixIcon,
        hintText: hintText,
        hintStyle: TextStyle(color: Colors.grey),
        filled: true,
        fillColor: const Color.fromARGB(255, 200, 199, 194),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(16),
          borderSide: BorderSide.none,
        ),
      ),
    );
  }
}
