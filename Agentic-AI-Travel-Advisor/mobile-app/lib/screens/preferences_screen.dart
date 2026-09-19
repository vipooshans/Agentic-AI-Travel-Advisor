import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../services/preferences_service.dart';
import '../widgets/auth_text_field.dart';
import '../widgets/error_widget.dart';
import '../widgets/loading_widget.dart';

class PreferencesScreen extends StatefulWidget {
  const PreferencesScreen({super.key});

  @override
  State<PreferencesScreen> createState() => _PreferencesScreenState();
}

class _PreferencesScreenState extends State<PreferencesScreen> {
  late Future<void> _load;
  final _min = TextEditingController();
  final _max = TextEditingController();
  final _climate = TextEditingController();
  final _interests = TextEditingController();
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    _load = _fetch();
  }

  Future<void> _fetch() async {
    final prefs = await PreferencesService(context.read<AuthProvider>().api).get();
    _min.text = prefs.budgetMin?.toStringAsFixed(0) ?? '';
    _max.text = prefs.budgetMax?.toStringAsFixed(0) ?? '';
    _climate.text = prefs.preferredClimate ?? '';
    _interests.text = prefs.interests ?? '';
  }

  @override
  void dispose() {
    _min.dispose();
    _max.dispose();
    _climate.dispose();
    _interests.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() => _saving = true);
    try {
      await PreferencesService(context.read<AuthProvider>().api).save(
        budgetMin: double.tryParse(_min.text.trim()),
        budgetMax: double.tryParse(_max.text.trim()),
        preferredClimate: _climate.text.trim().isEmpty ? null : _climate.text.trim(),
        interests: _interests.text.trim().isEmpty ? null : _interests.text.trim(),
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Preferences saved')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Travel preferences')),
      body: FutureBuilder(
        future: _load,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) return const LoadingWidget();
          if (snapshot.hasError) return ErrorDisplayWidget(message: snapshot.error.toString(), onRetry: () => setState(() => _load = _fetch()));
          return ListView(
            padding: const EdgeInsets.all(24),
            children: [
              AuthTextField(controller: _min, label: 'Minimum budget', keyboardType: TextInputType.number),
              const SizedBox(height: 12),
              AuthTextField(controller: _max, label: 'Maximum budget', keyboardType: TextInputType.number),
              const SizedBox(height: 12),
              AuthTextField(controller: _climate, label: 'Preferred climate'),
              const SizedBox(height: 12),
              AuthTextField(controller: _interests, label: 'Interests (e.g. hiking, beaches)'),
              const SizedBox(height: 24),
              FilledButton(
                onPressed: _saving ? null : _save,
                style: FilledButton.styleFrom(shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12))),
                child: Text(_saving ? 'Saving...' : 'Save preferences'),
              ),
            ],
          );
        },
      ),
    );
  }
}
