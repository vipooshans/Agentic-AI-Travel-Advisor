import 'package:flutter/material.dart';

/// An icon followed by a label as inline spans, so a long label wraps to the
/// next line instead of overflowing a fixed-width row.
InlineSpan iconLabel(IconData icon, String label, {Color? iconColor, TextStyle? style}) {
  return TextSpan(children: [
    WidgetSpan(
      alignment: PlaceholderAlignment.middle,
      child: Padding(
        padding: const EdgeInsets.only(right: 6),
        child: Icon(icon, size: 16, color: iconColor),
      ),
    ),
    TextSpan(text: label, style: style),
  ]);
}

const InlineSpan iconLabelGap = WidgetSpan(child: SizedBox(width: 16));
