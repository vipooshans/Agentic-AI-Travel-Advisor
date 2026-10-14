import 'package:flutter/material.dart';

import '../utils/validators.dart';

class CatalogSearch {
  final String query;
  final double? maxPrice;

  const CatalogSearch({this.query = '', this.maxPrice});

  bool get isEmpty => query.trim().isEmpty && maxPrice == null;
}

/// Keyword and maximum-price filters for the hotel and package lists.
class CatalogSearchBar extends StatefulWidget {
  final String hint;
  final String priceLabel;
  final ValueChanged<CatalogSearch> onSearch;

  const CatalogSearchBar({
    super.key,
    required this.hint,
    required this.priceLabel,
    required this.onSearch,
  });

  @override
  State<CatalogSearchBar> createState() => _CatalogSearchBarState();
}

class _CatalogSearchBarState extends State<CatalogSearchBar> {
  final _formKey = GlobalKey<FormState>();
  final _query = TextEditingController();
  final _price = TextEditingController();

  @override
  void dispose() {
    _query.dispose();
    _price.dispose();
    super.dispose();
  }

  void _submit() {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    final price = _price.text.trim();
    widget.onSearch(CatalogSearch(query: _query.text.trim(), maxPrice: price.isEmpty ? null : double.parse(price)));
  }

  void _clear() {
    _query.clear();
    _price.clear();
    _formKey.currentState?.validate();
    widget.onSearch(const CatalogSearch());
  }

  @override
  Widget build(BuildContext context) {
    return Form(
      key: _formKey,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 12, 12, 0),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              flex: 3,
              child: TextFormField(
                controller: _query,
                textInputAction: TextInputAction.search,
                onFieldSubmitted: (_) => _submit(),
                validator: (v) => Validators.maxLength(v, 100, 'Search'),
                decoration: InputDecoration(
                  hintText: widget.hint,
                  prefixIcon: const Icon(Icons.search),
                  isDense: true,
                  border: const OutlineInputBorder(),
                ),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              flex: 2,
              child: TextFormField(
                controller: _price,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                textInputAction: TextInputAction.search,
                onFieldSubmitted: (_) => _submit(),
                validator: Validators.optionalPrice,
                decoration: InputDecoration(
                  labelText: widget.priceLabel,
                  isDense: true,
                  border: const OutlineInputBorder(),
                ),
              ),
            ),
            IconButton(tooltip: 'Search', onPressed: _submit, icon: const Icon(Icons.arrow_forward)),
            IconButton(tooltip: 'Clear search', onPressed: _clear, icon: const Icon(Icons.clear)),
          ],
        ),
      ),
    );
  }
}
